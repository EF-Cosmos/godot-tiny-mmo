extends Node

static var instance: ChatServiceManager
var chat_client: ChatServiceClient
var fallback_enabled: bool = true

signal service_connected
signal service_disconnected
signal service_error(message: String)

func _enter_tree() -> void:
	if instance == null:
		instance = self

func _exit_tree() -> void:
	if instance == self:
		instance = null

# Initialize chat service manager
func initialize(player_id: int, username: String, display_name: String = "") -> void:
	var config = ConfigManager.get_config("chat-service")

	if not config.get("enabled", false):
		print("Chat service is disabled in configuration")
		return

	# Create chat client
	if chat_client:
		chat_client.queue_free()
		
	chat_client = ChatServiceClient.new(config)
	add_child(chat_client)

	# Connect signals
	chat_client.connection_established.connect(_on_connection_established)
	chat_client.connection_failed.connect(_on_connection_failed)
	chat_client.message_received.connect(_on_message_received)
	chat_client.private_message_received.connect(_on_private_message_received)
	chat_client.user_joined_room.connect(_on_user_joined_room)
	chat_client.user_left_room.connect(_on_user_left_room)
	chat_client.online_users_updated.connect(_on_online_users_updated)

	# Register user and connect
	chat_client.register_user(player_id, username, display_name)

# Send message using microservice (if available) or fallback
func send_message(room_id: int, content: String, message_type: String = "Text", fallback_callback: Callable = Callable()) -> bool:
	if chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED:
		chat_client.send_message(room_id, content, message_type)
		return true
	else:
		# Use fallback callback if provided
		if fallback_callback.is_valid():
			fallback_callback.call(room_id, content, message_type)
		return false

# Send private message
func send_private_message(recipient_id: int, content: String, fallback_callback: Callable = Callable()) -> bool:
	if chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED:
		chat_client.send_private_message(recipient_id, content)
		return true
	else:
		if fallback_callback.is_valid():
			fallback_callback.call(recipient_id, content)
		return false

# Join room
func join_room(room_id: int, username: String = "", fallback_callback: Callable = Callable()) -> bool:
	if chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED:
		chat_client.join_room(room_id, username)
		return true
	else:
		if fallback_callback.is_valid():
			fallback_callback.call(room_id, username)
		return false

# Leave room
func leave_room(room_id: int, fallback_callback: Callable = Callable()) -> bool:
	if chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED:
		chat_client.leave_room(room_id)
		return true
	else:
		if fallback_callback.is_valid():
			fallback_callback.call(room_id)
		return false

# Get room history
func get_room_history(room_id: int, limit: int = 50, before: String = "") -> Array:
	if chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED:
		return await chat_client.get_room_history(room_id, limit, before)
	else:
		# Return empty array if service is not available
		return []

# Poll WebSocket (should be called regularly)
func poll():
	if chat_client:
		chat_client.poll_websocket()

# Disconnect from chat service
func disconnect_service():
	if chat_client:
		chat_client.disconnect_websocket()

# Check if chat service is available
func is_available() -> bool:
	return chat_client and chat_client.connection_state == ChatServiceClient.ConnectionState.CONNECTED

# Get service status
func get_service_status() -> String:
	if not chat_client:
		return "Not initialized"

	match chat_client.connection_state:
		ChatServiceClient.ConnectionState.DISCONNECTED:
			return "Disconnected"
		ChatServiceClient.ConnectionState.CONNECTING:
			return "Connecting"
		ChatServiceClient.ConnectionState.CONNECTED:
			return "Connected"
		ChatServiceClient.ConnectionState.ERROR:
			return "Error"
		_:
			return "Unknown"

# Signal handlers
func _on_connection_established():
	print("Chat service connection established")
	service_connected.emit()

func _on_connection_failed(error: String):
	print("Chat service connection failed: %s" % error)
	service_error.emit(error)
	if fallback_enabled:
		print("Chat service unavailable, using fallback system")

func _on_message_received(message: Dictionary):
	# Convert microservice message format to Godot chat format
	var godot_message = {
		"text": message.get("content", ""),
		"channel": 0,  # Default to global channel for now
		"name": message.get("senderUsername", "Unknown"),
		"id": message.get("senderId", 0),
		"timestamp": message.get("timestamp", Time.get_unix_time_from_system())
	}

	# Forward to existing chat system
	_forward_to_godot_chat(godot_message)

func _on_private_message_received(message: Dictionary):
	# Handle private messages (could be displayed differently in UI)
	var godot_message = {
		"text": "[Private] " + message.get("content", ""),
		"channel": 0,
		"name": message.get("senderUsername", "Unknown"),
		"id": message.get("senderId", 0),
		"timestamp": message.get("timestamp", Time.get_unix_time_from_system())
	}

	_forward_to_godot_chat(godot_message)

func _on_user_joined_room(user_info: Dictionary):
	var system_message = {
		"text": "%s joined the room" % user_info.get("username", "Someone"),
		"channel": 0,
		"name": "System",
		"id": 1,
		"timestamp": Time.get_unix_time_from_system()
	}

	_forward_to_godot_chat(system_message)

func _on_user_left_room(user_info: Dictionary):
	var system_message = {
		"text": "%s left the room" % user_info.get("username", "Someone"),
		"channel": 0,
		"name": "System",
		"id": 1,
		"timestamp": Time.get_unix_time_from_system()
	}

	_forward_to_godot_chat(system_message)

func _on_online_users_updated(users: Array):
	# Could be used to update online user list in UI
	print("Online users updated: %d users" % users.size())

# Forward message to existing Godot chat system
func _forward_to_godot_chat(message: Dictionary):
	# This would integrate with the existing chat system
	# For now, just print the message
	var formatted_message = "[color=33caff]%s:[/color] %s" % [message.name, message.text]
	print(formatted_message)

# Get singleton instance
static func get_instance() -> ChatServiceManager:
	if instance == null:
		instance = ChatServiceManager.new()
	return instance

# Check if chat service is enabled in configuration
static func is_service_enabled() -> bool:
	var config = ConfigManager.get_config("chat-service")
	return config.get("enabled", false)