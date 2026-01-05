class_name InstanceManagerServer
extends SubViewportContainer


const INSTANCE_COLLECTION_PATH: String = "res://source/common/gameplay/maps/instance/instance_collection/"
const GLOBAL_COMMANDS_PATH: String = "res://source/server/world/components/chat_command/global_commands/"

var loading_instances: Dictionary[InstanceResource, ServerInstance]
var instance_collection: Array[InstanceResource]

@export var world_server: WorldServer


func start_instance_manager() -> void:
	ServerInstance.world_server = world_server
	
	setup_global_commands_and_roles()

	set_instance_collection.call_deferred()
	
	# Timer which will call unload_unused_instances
	var timer: Timer = Timer.new()
	timer.wait_time = 20.0 # 20.0 is for testing, consider increasing it
	
	timer.autostart = true
	timer.timeout.connect(unload_unused_instances)
	add_sibling(timer)

func setup_global_commands_and_roles() -> void:
	var files: PackedStringArray = FileUtils.get_all_file_at(GLOBAL_COMMANDS_PATH, "*.gd")
	if files.is_empty():
		return
	
	var commands := ServerInstance.global_chat_commands
	for file_path: String in files:
		var command = load(file_path).new()
		commands.set(command.command_name, command)

	var roles := ServerInstance.global_role_definitions
	for role: String in roles:
		var role_data: Dictionary = roles[role]
		var role_commands: Array
		
		for command_name: String in commands:
			var command = commands[command_name]
			if command.command_priority <= role_data.get("priority", 0):
				role_commands.append(command_name)

		role_data['commands'] = role_commands


@rpc("authority", "call_remote", "reliable", 0)
func charge_new_instance(_map_path: String, _instance_id: String) -> void:
	pass


func _on_player_entered_warper(player: Player, current_instance: ServerInstance, warper: Warper) -> void:
	var instance_index: int = -1 # Will be useful later
	var target_instance: ServerInstance
	var instance_resource: InstanceResource = warper.target_instance
	if not instance_resource:
		return
	
	if instance_resource.can_join_instance(player, instance_index):
		target_instance = instance_resource.get_instance()
		if target_instance:
			player_switch_instance(target_instance, warper.target_id, player, current_instance)
		else:
			queue_charge_instance(
				instance_resource,
				player,
				player_switch_instance.bind(warper.target_id, player, current_instance)
			)
	else:
		return


func queue_charge_instance(instance_resource: InstanceResource, player: Player, callback: Callable) -> void:
	if loading_instances.has(instance_resource):
		loading_instances[instance_resource].ready.connect(
			callback.bind(loading_instances[instance_resource])
		)
		return
	
	# MICROSERVICE LOGIC:
	# If we are here, it means the instance is NOT loaded locally.
	# In a monolithic server, we would load it now.
	# In a microservice architecture, we should check if another server is hosting it.
	
	var target_map_name = world_server.server_config.get("map_name", "")
	
	# If we are in "Single Server Mode" (target_map_name is empty), load it locally.
	if target_map_name == "":
		var new_instance: ServerInstance = prepare_instance(instance_resource)
		new_instance.ready.connect(callback.bind(new_instance), CONNECT_ONE_SHOT)
		add_child(new_instance, true)
		return

	# If we are in "Cluster Mode", try to find a remote server.
	print("Map '%s' not found locally. Initiating cross-server transfer for player %s..." % [instance_resource.instance_name, player.name])
	
	world_server.find_server_for_map(instance_resource.instance_name, func(server_info):
		if server_info:
			print("Found remote server: %s:%d" % [server_info.address, server_info.port])
			
			# Generate a temporary transfer token (in a real game, this should be validated by Auth Service)
			var transfer_token = "transfer_%s_%d" % [player.name, Time.get_ticks_msec()]
			
			# Tell the client to redirect
			# We use the RPC we added to world_client.gd
			# Note: 'redirect_to_server' must be an RPC on the client side.
			world_server.rpc_id(player.peer_id, "redirect_to_server", server_info.address, server_info.port, transfer_token)
			
			# Disconnect the player from this server gracefully?
			# The client will disconnect itself when connecting to the new server.
			# But we can also kick them after a short delay to ensure they leave.
		else:
			print("Could not find server for map: %s. Falling back to local load (Warning: This might crash if assets are missing)." % instance_resource.instance_name)
			# Fallback logic (optional, maybe we just want to fail)
			var new_instance: ServerInstance = prepare_instance(instance_resource)
			new_instance.ready.connect(callback.bind(new_instance), CONNECT_ONE_SHOT)
			add_child(new_instance, true)
	)


func player_switch_instance(
	target_instance: ServerInstance,
	warper_target_id: int,
	player: Player,
	current_instance: ServerInstance,
) -> void:
	var peer_id: int = player.name.to_int()
	if current_instance.connected_peers.has(peer_id):
		current_instance.despawn_player(peer_id, false)
	else:
		return
	charge_new_instance.rpc_id(
		peer_id,
		target_instance.instance_resource.map_path,
		target_instance.name
	)
	target_instance.awaiting_peers[peer_id] = {
		"player": player,
		"target_id": warper_target_id
	}


func charge_instance(instance_resource: InstanceResource) -> void:
	if loading_instances.has(instance_resource):
		return
	var new_instance: ServerInstance = prepare_instance(instance_resource)
	add_child.call_deferred(new_instance, true)


func prepare_instance(instance_resource: InstanceResource) -> ServerInstance:
	var instance: ServerInstance = ServerInstance.new()
	loading_instances[instance_resource] = instance
	instance.name = str(instance.get_instance_id())
	instance.instance_resource = instance_resource
	instance.player_entered_warper.connect(_on_player_entered_warper)
	instance.ready.connect(
		func():
			loading_instances.erase(instance_resource)
			instance_resource.charged_instances.append(instance),
		CONNECT_ONE_SHOT
	)
	instance.load_map(instance_resource.map_path)
	return instance


func set_instance_collection() -> void:
	var default_instance: InstanceResource
	
	# Get target map from config (default to "Overworld" or load all if empty)
	var target_map_name = world_server.server_config.get("map_name", "")
	print("InstanceManager: Target map is '%s'" % target_map_name)
	
	for file_path: String in FileUtils.get_all_file_at(INSTANCE_COLLECTION_PATH, "*.tres"):
		# print(file_path)
		instance_collection.append(ResourceLoader.load(file_path, "InstanceResource"))
	
	for instance_resource: InstanceResource in instance_collection:
		# Logic:
		# 1. If target_map_name is set, ONLY load that map.
		# 2. If target_map_name is empty, load everything marked 'load_at_startup' (Legacy/Dev mode)
		
		var should_load = false
		
		if target_map_name != "":
			if instance_resource.instance_name == target_map_name:
				should_load = true
				default_instance = instance_resource # Set this as default for this server
		else:
			if instance_resource.load_at_startup:
				should_load = true
			if instance_resource.instance_name == "Overworld":
				default_instance = instance_resource

		if should_load:
			print("Loading instance: %s" % instance_resource.instance_name)
			charge_instance(instance_resource)
	
	if not default_instance and not instance_collection.is_empty():
		# Fallback if no default found (e.g. map name typo), pick the first one loaded
		for res in instance_collection:
			if not res.charged_instances.is_empty():
				default_instance = res
				break
				
	if default_instance:
		print("Default instance set to: %s" % default_instance.instance_name)
		world_server.multiplayer_api.peer_connected.connect(
			func(peer_id: int):
				# Wait for instance to be fully charged if it's the very first connection
				if default_instance.charged_instances.is_empty():
					await get_tree().create_timer(1.0).timeout # Simple retry wait
					
				if not default_instance.charged_instances.is_empty():
					charge_new_instance.rpc_id(
						peer_id,
						default_instance.map_path,
						default_instance.charged_instances[0].name
					)
				else:
					printerr("Error: No instance ready for player %d" % peer_id)
		)
	else:
		printerr("CRITICAL: No default instance found! Players cannot spawn.")


func unload_unused_instances() -> void:
	print("Checking unload_unused_instances")
	for instance: ServerInstance in get_children():
		if instance.instance_resource.load_at_startup:
			continue
		if instance.connected_peers:
			continue
		instance.instance_resource.charged_instances.erase(instance)
		instance.queue_free()
