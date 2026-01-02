class_name ChatServiceClient
extends Node

signal message_received(message: Dictionary)
signal private_message_received(message: Dictionary)
signal user_joined_room(user_info: Dictionary)
signal user_left_room(user_info: Dictionary)
signal online_users_updated(users: Array)
signal connection_established
signal connection_failed(error: String)

var websocket_client: WebSocketPeer
var chat_service_config: Dictionary
var jwt_token: String
var current_user_id: int
var current_username: String
var connected_rooms: Array[int] = []

# WebSocket connection state
enum ConnectionState {
	DISCONNECTED,
	CONNECTING,
	CONNECTED,
	ERROR
}

var connection_state: ConnectionState = ConnectionState.DISCONNECTED

func _init(config: Dictionary = {}):
	chat_service_config = config
	websocket_client = WebSocketPeer.new()

func _ready() -> void:
	set_process(false) # Only process when connected/connecting

func _process(_delta: float) -> void:
	websocket_client.poll()
	var state = websocket_client.get_ready_state()
	
	if state == WebSocketPeer.STATE_OPEN:
		if connection_state != ConnectionState.CONNECTED:
			connection_state = ConnectionState.CONNECTED
			connection_established.emit()
			print("Chat WebSocket Connected")
		
		while websocket_client.get_available_packet_count():
			var packet = websocket_client.get_packet()
			var message = packet.get_string_from_utf8()
			_handle_websocket_message(message)
			
	elif state == WebSocketPeer.STATE_CLOSED:
		if connection_state == ConnectionState.CONNECTED:
			connection_state = ConnectionState.DISCONNECTED
			connection_failed.emit("Connection closed")
			set_process(false)
			print("Chat WebSocket Closed")

# Register user with the chat service
func register_user(user_id: int, username: String, display_name: String = "") -> void:
	current_user_id = user_id
	current_username = username

	# Generate JWT token (in production, this should come from authentication service)
	jwt_token = _generate_jwt_token(user_id, username)

	var url = "http://%s:%d/api/chat/users/%d/register" % [
		chat_service_config.get("address", "127.0.0.1"),
		chat_service_config.get("port", 5004),
		user_id
	]

	var headers = [
		"Content-Type: application/json",
		"Authorization: Bearer %s" % jwt_token
	]

	var body_data = {
		"username": username,
		"email": "",
		"avatar": ""
	}

	var http = HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_registration_completed.bind(http))
	
	var error = http.request(url, headers, HTTPClient.METHOD_POST, JSON.stringify(body_data))
	if error != OK:
		print("Failed to send registration request")
		http.queue_free()

func _on_registration_completed(result, response_code, headers, body, http: HTTPRequest) -> void:
	http.queue_free()
	if response_code == 200:
		print("User %s registered with chat service" % current_username)
		connect_websocket()
	else:
		print("Failed to register with chat service: %d" % response_code)

# Connect to WebSocket
func connect_websocket() -> void:
	connection_state = ConnectionState.CONNECTING
	
	# SignalR uses /chatHub endpoint
	# Note: Godot WebSocketPeer is raw WebSocket. SignalR has a protocol.
	# For simplicity, we might need to use a raw WebSocket endpoint on the server or implement SignalR handshake.
	# Assuming the server exposes a raw WebSocket or we speak SignalR JSON protocol.
	# Let's assume we use the raw websocket for now, but SignalR requires negotiation.
	# If the server is SignalR, we should probably use a SignalR client library or implement the handshake.
	# For this migration, let's assume we just connect to the hub.
	
	var ws_url = "ws://%s:%d%s?access_token=%s" % [
		chat_service_config.get("address", "127.0.0.1"),
		chat_service_config.get("port", 5004),
		chat_service_config.get("ws_endpoint", "/chatHub"),
		jwt_token
	]

	print("Connecting to Chat WebSocket: %s" % ws_url)
	var error = websocket_client.connect_to_url(ws_url)
	if error == OK:
		set_process(true)
	else:
		connection_failed.emit("Failed to connect")

func _handle_websocket_message(message: String) -> void:
	# SignalR messages are JSON.
	# They end with 0x1E (Record Separator).
	# We need to parse them.
	# For now, just print.
	print("Received: %s" % message)
	
	# TODO: Parse SignalR protocol
	# If it's a simple chat message:
	# message_received.emit(parsed_message)

func send_message(text: String, channel: int) -> void:
	if connection_state != ConnectionState.CONNECTED:
		return
		
	# Send as JSON for now
	var data = {
		"text": text,
		"channel": channel
	}
	websocket_client.put_packet(JSON.stringify(data).to_utf8_buffer())

func _generate_jwt_token(user_id: int, username: String) -> String:
	# Mock token for dev
	return "mock_token_%d_%s" % [user_id, username]
			return false

	if websocket_client.get_ready_state() == WebSocketPeer.STATE_OPEN:
		connection_state = ConnectionState.CONNECTED
		connection_established.emit()
		print("Connected to chat WebSocket")
		return true
	else:
		connection_state = ConnectionState.ERROR
		var error_msg = "WebSocket connection failed: %d" % websocket_client.get_ready_state()
		connection_failed.emit(error_msg)
		print(error_msg)
		return false

# Join a chat room
func join_room(room_id: int, username: String = ""):
	if connection_state != ConnectionState.CONNECTED:
		print("Cannot join room: not connected to chat service")
		return

	var join_data = {
		"roomId": room_id,
		"username": username if username != "" else current_username
	}

	_send_websocket_message("JoinRoom", join_data)
	connected_rooms.append(room_id)

# Leave a chat room
func leave_room(room_id: int):
	if connection_state != ConnectionState.CONNECTED:
		return

	_send_websocket_message("LeaveRoom", {"roomId": room_id})
	connected_rooms.erase(room_id)

# Send a message to a room
func send_message(room_id: int, content: String, message_type: String = "Text"):
	if connection_state != ConnectionState.CONNECTED:
		print("Cannot send message: not connected to chat service")
		return

	var message_data = {
		"roomId": room_id,
		"content": content,
		"messageType": message_type
	}

	_send_websocket_message("SendMessage", message_data)

# Send a private message
func send_private_message(recipient_id: int, content: String):
	if connection_state != ConnectionState.CONNECTED:
		print("Cannot send private message: not connected to chat service")
		return

	var message_data = {
		"recipientId": recipient_id,
		"content": content
	}

	_send_websocket_message("SendPrivateMessage", message_data)

# Get online users in a room
func get_online_users(room_id: int):
	if connection_state != ConnectionState.CONNECTED:
		return

	_send_websocket_message("GetOnlineUsers", {"roomId": room_id})

# Get room message history via HTTP
async func get_room_history(room_id: int, limit: int = 50, before: String = "") -> Array:
	var url = "http://%s:%d/api/chat/rooms/%d/messages?limit=%d" % [
		chat_service_config.get("address", "127.0.0.1"),
		chat_service_config.get("port", 8090),
		room_id,
		limit
	]

	if before != "":
		url += "&before=%s" % before

	var headers = ["Authorization: Bearer %s" % jwt_token]

	http_client.request(HTTPClient.METHOD_GET, url, headers)

	# Wait for response
	var start_time = Time.get_ticks_msec()
	while http_client.get_status() == HTTPClient.STATUS_REQUESTING:
		await get_tree().process_frame
		if Time.get_ticks_msec() - start_time > 5000:  # 5 second timeout
			print("Get room history timeout")
			return []

	if http_client.get_response_code() == 200:
		var body = http_client.read_response_body_chunk()
		var json = JSON.new()
		var parse_result = json.parse(body.get_string_from_utf8())

		if parse_result == OK:
			var response_data = json.data
			if response_data.has("messages"):
				return response_data.messages
		else:
			print("Failed to parse room history response")

	return []

# Poll WebSocket for incoming messages
func poll_websocket():
	if connection_state != ConnectionState.CONNECTED:
		return

	websocket_client.poll()

	while websocket_client.get_ready_state() == WebSocketPeer.STATE_OPEN:
		var packet = websocket_client.get_packet()
		if packet.size() > 0:
			_handle_websocket_message(packet)
		else:
			break

# Disconnect from chat service
func disconnect():
	if websocket_client.get_ready_state() == WebSocketPeer.STATE_OPEN:
		websocket_client.close()

	connection_state = ConnectionState.DISCONNECTED
	connected_rooms.clear()

# Internal methods
func _send_websocket_message(method: String, data: Dictionary):
	var message_data = {
		"type": method,
		"arguments": [data]
	}

	var json = JSON.new()
	var message_string = json.stringify(message_data)
	websocket_client.send_text(message_string)

func _handle_websocket_message(packet: PackedByteArray):
	var message_string = packet.get_string_from_utf8()
	var json = JSON.new()
	var parse_result = json.parse(message_string)

	if parse_result != OK:
		print("Failed to parse WebSocket message: %s" % message_string)
		return

	var message_data = json.data

	# Handle different message types
	if message_data.has("target") and message_data.has("arguments"):
		var target = message_data.target
		var args = message_data.arguments

		match target:
			"ReceiveMessage":
				if args.size() > 0:
					message_received.emit(args[0])
			"ReceivePrivateMessage":
				if args.size() > 0:
					private_message_received.emit(args[0])
			"UserJoined":
				if args.size() > 0:
					user_joined_room.emit(args[0])
			"UserLeft":
				if args.size() > 0:
					user_left_room.emit(args[0])
			"OnlineUsers":
				if args.size() > 0:
					online_users_updated.emit(args[0])

# Generate simple JWT token (in production, use proper JWT library)
func _generate_jwt_token(user_id: int, username: String) -> String:
	# This is a very simplified JWT token for demonstration
	# In production, use a proper JWT library and secure key
	var header = {"alg": "HS256", "typ": "JWT"}
	var payload = {
		"sub": str(user_id),
		"name": username,
		"iat": Time.get_unix_time_from_system(),
		"exp": Time.get_unix_time_from_system() + 3600,  # 1 hour expiry
		"iss": chat_service_config.get("jwt_issuer", "Game.ChatService"),
		"aud": "Game.TinyMMO"
	}

	# In a real implementation, you would:
	# 1. Base64Url encode header and payload
	# 2. Create signature with HMAC-SHA256
	# 3. Combine header.payload.signature

	# For now, return a simple token format
	return "simple-jwt-token-%d-%s" % [user_id, username]

# Check if chat service is enabled
static func is_enabled() -> bool:
	var config = ConfigManager.get_config("chat-service")
	return config.get("enabled", false)