

## Shared keys (client + gateway)
const KEY_TOKEN_ID: String = "t-id"
const KEY_ACCOUNT_ID: String = "a-id"
const KEY_ACCOUNT_USERNAME: String = "a-u"
const KEY_WORLD_ID: String = "w-id"
const KEY_CHAR_ID: String = "c-id"


static func base_url() -> String:
	# Hardcoded default one
	return "http://localhost:5000"
	#var command_line_arg: String = CmdlineUtils.get_parsed_args().get("api", "")
	#if command_line_arg:
		#return command_line_arg
#
	## Check if has default in ProjectSettings
	## (set different values for debug/release export presets))
	#var value: String = ProjectSettings.get_setting("network/api/base_url", "")
	#if not value.is_empty():
		#return value
	#return "http://127.0.0.1:8088"


static func get_endpoint(path: String) -> String:
	return "%s%s" % [base_url().rstrip("/"), path]


# Endpoints
static func login() -> String: return get_endpoint("/api/auth/login")
static func guest() -> String: return get_endpoint("/api/auth/guest")
static func worlds() -> String: return get_endpoint("/api/game/worlds")
static func account_create() -> StringName:
		return get_endpoint(&"/api/auth/register")
static func world_characters() -> String: return get_endpoint("/api/game/world/characters")
static func world_enter() -> String: return get_endpoint("/api/game/world/enter")
static func world_create_char() -> String: return get_endpoint("/api/game/world/character/create")
