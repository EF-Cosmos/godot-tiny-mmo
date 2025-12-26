extends DataRequestHandler

# Chat service manager instance (will be initialized if needed)
var chat_service_manager: ChatServiceManager


func _init():
	# Initialize chat service manager if enabled
	if ChatServiceManager.is_service_enabled():
		chat_service_manager = ChatServiceManager.get_instance()


func data_request_handler(
	peer_id: int,
	instance: ServerInstance,
	args: Dictionary
) -> Dictionary:
	var player_resource = instance.players_by_peer_id[peer_id].player_resource
	var text: String = args.get("text", "")
	var channel: int = args.get("channel", 0)

	# Validate message
	if text.is_empty():
		return {"error": "Message cannot be empty"}

	# Try to use microservice if available
	if chat_service_manager and chat_service_manager.is_available():
		# Convert channel to room ID (for now, use a simple mapping)
		var room_id: int = _channel_to_room_id(channel)

		# Send via microservice
		if chat_service_manager.send_message(room_id, text, "Text", _fallback_send_message.bind(instance, peer_id, args)):
			# Message sent via microservice successfully
			return {}
		else:
			print("Failed to send message via microservice, falling back to local")

	# Fallback to local message handling
	return _fallback_send_message(instance, peer_id, args)


func _fallback_send_message(instance: ServerInstance, peer_id: int, args: Dictionary) -> Dictionary:
	var message: Dictionary = {
		"text": args.get("text", ""),
		"channel": args.get("channel", 0),
		"name": instance.players_by_peer_id[peer_id].player_resource.display_name,
		"id": peer_id,
		"timestamp": Time.get_unix_time_from_system()
	}
	instance.propagate_rpc(instance.data_push.bind(&"chat.message", message))
	return {} # ACK later


# Convert channel ID to room ID for microservice
func _channel_to_room_id(channel: int) -> int:
	# Simple mapping for now
	# 0 = Global chat room
	# 1 = Trade chat room
	# Other channels could be mapped to guild rooms, private rooms, etc.
	match channel:
		0:
			return 1  # Global room ID
		1:
			return 2  # Trade room ID
		_:
			return 1  # Default to global


# Initialize chat service for a player
func initialize_player_chat_service(peer_id: int, instance: ServerInstance):
	if not chat_service_manager:
		return

	var player_resource = instance.players_by_peer_id[peer_id].player_resource
	if not player_resource:
		return

	# Initialize chat service asynchronously (don't block the main thread)
	_initialize_chat_service_async(peer_id, instance, player_resource.player_id, player_resource.display_name)


func _initialize_chat_service_async(peer_id: int, instance: ServerInstance, player_id: int, display_name: String):
	# This would be called asynchronously to avoid blocking
	if await chat_service_manager.initialize(player_id, display_name, display_name):
		# Join default rooms
		chat_service_manager.join_room(_channel_to_room_id(0), display_name)  # Global
		chat_service_manager.join_room(_channel_to_room_id(1), display_name)  # Trade


# Poll chat service (should be called regularly from the main server loop)
func poll_chat_service():
	if chat_service_manager:
		chat_service_manager.poll()
