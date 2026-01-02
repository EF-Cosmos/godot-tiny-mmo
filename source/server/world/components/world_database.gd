class_name WorldDatabase
extends Node

signal player_ready(character_id: int, player: PlayerResource)
signal player_load_failed(character_id: int)

var player_data: WorldPlayerData
var game_service: GameServiceClient
var pending_requests: Dictionary[int, Dictionary] = {}

func _ready() -> void:
	game_service = GameServiceClient.new()
	add_child(game_service)
	game_service.player_data_received.connect(_on_player_data_received)

func start_database(_world_info: Dictionary) -> void:
	# Initialize empty runtime cache
	player_data = WorldPlayerData.new()
	player_data.players = {}
	print("WorldDatabase started in Online Mode")

func fetch_player_data(character_id: int, account_name: String = "") -> void:
	# Check cache first
	if player_data.players.has(character_id):
		player_ready.emit(character_id, player_data.players[character_id])
		return
	
	print("Fetching data for player %d from GameService..." % character_id)
	pending_requests[character_id] = { "account_name": account_name }
	game_service.get_player_inventory(character_id)

func _on_player_data_received(character_id: int, data: Dictionary) -> void:
	print("Received data for player %d" % character_id)
	var context = pending_requests.get(character_id, {})
	pending_requests.erase(character_id)
	
	var player = PlayerResource.new()
	player.player_id = character_id
	player.account_name = context.get("account_name", "")
	# Default values for now since we only fetch inventory
	player.display_name = "Player %d" % character_id
	player.skin_id = 1
	
	# Populate inventory from C# response
	player.inventory = {}
	if data.has("items"):
		for item in data["items"]:
			# Map C# item to Godot item structure
			# Godot: { item_id: { "stack": count } }
			var item_id = item["itemId"]
			var quantity = item["quantity"]
			player.inventory[item_id] = { "stack": quantity }
	
	player_data.players[character_id] = player
	player_ready.emit(character_id, player)

func add_item(character_id: int, item_id: int, quantity: int) -> void:
	# Optimistic update
	if player_data.players.has(character_id):
		var player = player_data.players[character_id]
		if player.inventory.has(item_id):
			player.inventory[item_id]["stack"] += quantity
		else:
			player.inventory[item_id] = { "stack": quantity }
			
	# Send to backend
	game_service.add_item(character_id, item_id, quantity)

func remove_item(character_id: int, item_id: int, quantity: int) -> void:
	# Optimistic update
	if player_data.players.has(character_id):
		var player = player_data.players[character_id]
		if player.inventory.has(item_id):
			player.inventory[item_id]["stack"] -= quantity
			if player.inventory[item_id]["stack"] <= 0:
				player.inventory.erase(item_id)
				
	# Send to backend
	game_service.remove_item(character_id, item_id, quantity)

func save_world_database() -> void:
	# TODO: Implement saving back to C# service
	pass

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST:
		save_world_database()
