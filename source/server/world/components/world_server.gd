class_name WorldServer
extends BaseMultiplayerEndpoint
## Server autoload. Keep it clean and minimal.
## Should only care about connection and authentication stuff.

@export var database: WorldDatabase
@export var world_manager: WorldManagerClient
@export var world_clock: WorldClockServer

var token_list: Dictionary[String, PlayerResource]
var pending_logins: Dictionary[int, String] = {}

var connected_players: Dictionary[int, PlayerResource]

# Room Service Integration
var room_service_url: String = "http://localhost:5003/api/server" # Default, should be from config
var server_id: String = ""
var heartbeat_timer: Timer
var server_config: Dictionary


func start_world_server() -> void:
	# Connect to database signal
	database.player_ready.connect(_on_player_data_ready)

	world_manager.token_received.connect(
		func(auth_token: String, username: String, character_id: int):
			print("Token received for char %d. Fetching data..." % character_id)
			pending_logins[character_id] = auth_token
			database.fetch_player_data(character_id, username)
	)

	server_config = ConfigFileUtils.load_section(
		"world-server",
		CmdlineUtils.get_parsed_args().get("config", "res://data/config/world_config.cfg")
	)
	if server_config.has("error"):
		printerr("Failed to load world server configuration!")
	else:
		print("Starting World Server on port: %d" % server_config.port)
		create(Role.SERVER, server_config.bind_address, server_config.port)
		
		# Register to Room Service
		_register_to_room_service()
	
	$InstanceManager.start_instance_manager()


func _register_to_room_service() -> void:
	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_registration_completed)
	
	var body = JSON.stringify({
		"name": server_config.get("name", "Unknown Server"),
		"address": server_config.bind_address, # Or public IP if needed
		"port": server_config.port,
		"region": "default",
		"maxPlayers": server_config.get("max_players", 100),
		"mapName": server_config.get("map_name", "default_map")
	})
	
	var headers = ["Content-Type: application/json"]
	var error = http.request(room_service_url + "/register", headers, HTTPClient.METHOD_POST, body)
	if error != OK:
		printerr("Failed to send registration request to Room Service.")


func _on_registration_completed(result, response_code, headers, body):
	if response_code == 200:
		var response = JSON.parse_string(body.get_string_from_utf8())
		server_id = response.get("serverId", "")
		print("Successfully registered to Room Service. Server ID: %s" % server_id)
		_start_heartbeat()
	else:
		printerr("Failed to register to Room Service. Code: %d" % response_code)


func _start_heartbeat() -> void:
	heartbeat_timer = Timer.new()
	heartbeat_timer.wait_time = 10.0 # Send heartbeat every 10 seconds
	heartbeat_timer.autostart = true
	heartbeat_timer.timeout.connect(_send_heartbeat)
	add_child(heartbeat_timer)
	_send_heartbeat() # Send first one immediately


func _send_heartbeat() -> void:
	if server_id.is_empty(): return
	
	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(func(result, code, headers, body): http.queue_free())
	
	var body = JSON.stringify({
		"serverId": server_id,
		"currentPlayers": connected_players.size()
	})
	
	var headers = ["Content-Type: application/json"]
	http.request(room_service_url + "/heartbeat", headers, HTTPClient.METHOD_POST, body)


func _connect_multiplayer_api_signals(api: SceneMultiplayer) -> void:
	api.peer_connected.connect(_on_peer_connected)
	api.peer_disconnected.connect(_on_peer_disconnected)
	
	api.peer_authenticating.connect(_on_peer_authenticating)
	api.peer_authentication_failed.connect(_on_peer_authentication_failed)
	api.set_auth_callback(_authentication_callback)


func _on_peer_connected(peer_id: int) -> void:
	print("Peer: %d is connected. Waiting for authentication..." % peer_id)


func _on_peer_disconnected(peer_id: int) -> void:
	print("Peer: %d is disconnected." % peer_id)
	if connected_players.has(peer_id):
		world_manager.player_disconnected.rpc_id(
			1,
			connected_players[peer_id].account_name
		)
		connected_players.erase(peer_id)


func _on_peer_authenticating(peer_id: int) -> void:
	print("Peer: %d is trying to authenticate." % peer_id)
	multiplayer.send_auth(peer_id, "TinyMMO_Auth_Challenge".to_ascii_buffer())


func _on_peer_authentication_failed(peer_id: int) -> void:
	print("Peer: %d failed to authenticate. Disconnecting..." % peer_id)
	peer.disconnect_peer(peer_id)


func _authentication_callback(peer_id: int, data: PackedByteArray) -> void:
	var auth_token := bytes_to_var(data) as String
	print("Peer: %d sent auth token: \"%s\"." % [peer_id, auth_token])
	if is_valid_authentication_token(auth_token):
		print("Authentication successful for Peer: %d" % peer_id)
		multiplayer.complete_auth(peer_id)
		connected_players[peer_id] = token_list[auth_token]
		token_list.erase(auth_token)
	else:
		print("Authentication FAILED for Peer: %d. Invalid token." % peer_id)
		peer.disconnect_peer(peer_id)


func is_valid_authentication_token(auth_token: String) -> bool:
	if token_list.has(auth_token):
		return true
	return false


func _on_player_data_ready(character_id: int, player: PlayerResource) -> void:
	if pending_logins.has(character_id):
		var auth_token = pending_logins[character_id]
		pending_logins.erase(character_id)
		
		token_list[auth_token] = player
		print("Received valid token for player: %s (CharID: %d)" % [player.display_name, character_id])
		
		# Token expires after 30 seconds if not used
		get_tree().create_timer(30.0).timeout.connect(func():
			if token_list.has(auth_token):
				print("Token expired for player: %s" % player.display_name)
				token_list.erase(auth_token)
		)
