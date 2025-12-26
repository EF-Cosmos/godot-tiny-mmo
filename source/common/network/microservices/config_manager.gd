class_name ConfigManager
extends RefCounted

# Cache for loaded configurations
var config_cache: Dictionary = {}

# Get configuration for a specific microservice
static func get_config(service_name: String) -> Dictionary:
	var instance = ConfigManager.new()
	return instance._get_service_config(service_name)

# Internal method to get service configuration with caching
func _get_service_config(service_name: String) -> Dictionary:
	# Check cache first
	if config_cache.has(service_name):
		return config_cache[service_name]

	# Default configuration based on service type
	var defaults = _get_service_defaults(service_name)

	# Try to load from master config (most services configured there)
	var config = _load_from_config_file("data/config/master_config.cfg", service_name, defaults)

	# If not found in master config, try other config files
	if config == defaults:
		config = _load_from_config_file("data/config/world_config.cfg", service_name, defaults)
		if config == defaults:
			config = _load_from_config_file("data/config/gateway_config.cfg", service_name, defaults)

	# Cache the configuration
	config_cache[service_name] = config

	return config

# Load configuration from a specific config file
func _load_from_config_file(config_path: String, service_name: String, defaults: Dictionary) -> Dictionary:
	var config_file: ConfigFile = ConfigFile.new()
	var error: Error = config_file.load(config_path)

	if error != OK:
		print("Warning: Could not load config file %s, using defaults for %s" % [config_path, service_name])
		return defaults

	if not config_file.has_section(service_name):
		return defaults

	var config = defaults.duplicate(true)

	for key: String in config_file.get_section_keys(service_name):
		config[key] = config_file.get_value(service_name, key, defaults.get(key))

	return config

# Get default configuration for each service type
func _get_service_defaults(service_name: String) -> Dictionary:
	match service_name:
		"chat-service":
			return {
				"enabled": false,
				"address": "127.0.0.1",
				"port": 8090,
				"protocol": "http",
				"ws_endpoint": "/chatHub",
				"jwt_key": "development-key-for-jwt-signing-change-in-production",
				"jwt_issuer": "Game.ChatService",
				"timeout": 5000
			}
		"analytics-service":
			return {
				"enabled": false,
				"address": "127.0.0.1",
				"port": 8093,
				"protocol": "http",
				"timeout": 3000
			}
		"matchmaking-service":
			return {
				"enabled": false,
				"address": "127.0.0.1",
				"port": 8091,
				"protocol": "http",
				"timeout": 10000
			}
		"leaderboard-service":
			return {
				"enabled": false,
				"address": "127.0.0.1",
				"port": 8092,
				"protocol": "grpc",
				"timeout": 5000
			}
		_:
			return {}

# Check if a service is enabled
static func is_service_enabled(service_name: String) -> bool:
	var config = get_config(service_name)
	return config.get("enabled", false)

# Get service URL
static func get_service_url(service_name: String, use_websocket: bool = false) -> String:
	var config = get_config(service_name)
	var protocol = config.get("protocol", "http")
	var address = config.get("address", "127.0.0.1")
	var port = config.get("port", 8080)

	if use_websocket and protocol == "http":
		protocol = "ws"
	elif use_websocket and protocol == "https":
		protocol = "wss"

	var endpoint = ""
	if use_websocket and config.has("ws_endpoint"):
		endpoint = config.get("ws_endpoint", "")

	return "%s://%s:%d%s" % [protocol, address, port, endpoint]

# Get service timeout in milliseconds
static func get_service_timeout(service_name: String) -> int:
	var config = get_config(service_name)
	return config.get("timeout", 5000)