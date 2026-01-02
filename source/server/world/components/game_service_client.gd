class_name GameServiceClient
extends Node

signal player_data_received(character_id: int, data: Dictionary)
signal inventory_updated(character_id: int, success: bool)

var base_url: String = "http://localhost:5004" # Default, override in config

func _ready() -> void:
	# Try to load config
	var args = CmdlineUtils.get_parsed_args()
	var config_path = args.get("config", "res://data/config/world_config.cfg")
	var config = ConfigFileUtils.load_section("game-service", config_path)
	if not config.has("error"):
		base_url = config.get("url", base_url)
	print("GameServiceClient initialized with URL: %s" % base_url)

func get_player_inventory(character_id: int) -> void:
	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_get_inventory_completed.bind(http, character_id))
	
	var url = "%s/api/inventory/%d" % [base_url, character_id]
	var error = http.request(url)
	if error != OK:
		printerr("Failed to request inventory for character %d" % character_id)
		http.queue_free()

func _on_get_inventory_completed(result, response_code, headers, body, http: HTTPRequest, character_id: int) -> void:
	http.queue_free()
	if response_code == 200:
		var json = JSON.parse_string(body.get_string_from_utf8())
		if json:
			player_data_received.emit(character_id, json)
	else:
		printerr("Failed to get inventory. Code: %d" % response_code)

func add_item(character_id: int, item_id: int, quantity: int, scope: String = "Global") -> void:
	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_inventory_update_completed.bind(http, character_id))
	
	var url = "%s/api/inventory/%d/items" % [base_url, character_id]
	var headers = ["Content-Type: application/json"]
	var data = {
		"itemId": item_id,
		"quantity": quantity,
		"scope": scope
	}
	var error = http.request(url, headers, HTTPClient.METHOD_POST, JSON.stringify(data))
	if error != OK:
		printerr("Failed to add item for character %d" % character_id)
		http.queue_free()

func remove_item(character_id: int, item_id: int, quantity: int, scope: String = "Global") -> void:
	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_inventory_update_completed.bind(http, character_id))
	
	var url = "%s/api/inventory/%d/items/%d?quantity=%d&scope=%s" % [base_url, character_id, item_id, quantity, scope]
	var headers = ["Content-Type: application/json"]
	var error = http.request(url, headers, HTTPClient.METHOD_DELETE)
	if error != OK:
		printerr("Failed to remove item for character %d" % character_id)
		http.queue_free()

func _on_inventory_update_completed(result, response_code, headers, body, http: HTTPRequest, character_id: int) -> void:
	http.queue_free()
	if response_code == 200:
		inventory_updated.emit(character_id, true)
	else:
		printerr("Inventory update failed. Code: %d" % response_code)
		inventory_updated.emit(character_id, false)
