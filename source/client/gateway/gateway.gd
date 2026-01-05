extends Control#can be refactor, 350 lines script too much?


# Helper class
const CredentialsUtils = preload("res://source/common/utils/credentials_utils.gd")
const GatewayApi = preload("res://source/common/network/gateway_api.gd")

@export var world_server: WorldClient

var account_id: int
var account_name: String
var token: int = randi()
var jwt_token: String = ""

var current_world_id: int
var selected_skin_id: int

var menu_stack: Array[Control]

@onready var main_panel: PanelContainer = $MainPanel
@onready var login_panel: PanelContainer = $LoginPanel
@onready var popup_panel: PanelContainer = $PopupPanel

@onready var back_button: Button = $BackButton

@onready var http_request: HTTPRequest = $HTTPRequest


func _ready() -> void:
	$SwapButton.toggled.connect(func(toggled_on: bool):
		if not $AudioStreamPlayer.playing:
			$AudioStreamPlayer.play()
		$Desert.visible = toggled_on == true
		$FairyForest.visible = toggled_on == false
		if toggled_on:
			BetterThemeDB.theme = load("res://source/client/ui/themes/theme_desert.tres")
		else:
			BetterThemeDB.theme = preload("res://source/client/ui/themes/theme_navy.tres")
		theme = BetterThemeDB.theme
	)
	menu_stack.append(main_panel)
	back_button.hide()
	back_button.pressed.connect(func():
		if menu_stack.size():
			menu_stack.pop_back().hide()
			if menu_stack.size():
				menu_stack.back().show()
			if menu_stack.size() < 2:
				back_button.hide()
		)
	
	var animated_sprite_2d: AnimatedSprite2D = $CharacterCreation/VBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer2/CenterContainer/Control/AnimatedSprite2D
	animated_sprite_2d.play(&"run")
	var v_box_container: GridContainer = $CharacterCreation/VBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/VBoxContainer
	for button: Button in v_box_container.get_children():
		button.pressed.connect(
		func():
			var sprite: SpriteFrames = ContentRegistryHub.load_by_slug(&"sprites", button.text.to_lower()) as SpriteFrames
			if not sprite:
				return
			selected_skin_id =  ContentRegistryHub.id_from_slug(&"sprites", button.text.to_lower())
			animated_sprite_2d.sprite_frames = sprite
			animated_sprite_2d.play(&"run")
		)


func do_request(
	method: HTTPClient.Method,
	path: String,
	payload: Dictionary,
) -> Variant:
	if http_request.get_http_client_status() == HTTPClient.Status.STATUS_CONNECTED:
		return {"error": "request_error"}
	
	var custom_headers: PackedStringArray
	custom_headers.append("Content-Type: application/json")
	if not jwt_token.is_empty():
		custom_headers.append("Authorization: Bearer " + jwt_token)
	
	var error: Error = http_request.request(
		path,
		custom_headers,
		method, # Use the passed method
		JSON.stringify(payload)
	)

	if error != OK:
		push_error("An error occurred in the HTTP request.")
		return {ok=false, error="request_error", code=error}
	
	var args: Array = await http_request.request_completed
	var result: int = args[0]
	if result != OK:
		print("ERROR?, TIMEOUT?")
		return {"error": 1, "ERROR?": "TIMEOUT?"}
	
	var response_code: int = args[1]
	var headers: PackedStringArray = args[2]
	var body: PackedByteArray = args[3]
	
	var json = JSON.new()
	var parse_result = json.parse(body.get_string_from_ascii())
	
	if parse_result == OK:
		return json.data
	
	print("JSON Parse Error: ", json.get_error_message())
	return {"error": 1}


func _show(next: Control, can_back: bool = true) -> void:
	if menu_stack.size():
		menu_stack.back().hide()
	if not can_back:
		menu_stack.clear()
	next.show()
	menu_stack.append(next)
	back_button.visible = can_back


func _on_login_button_pressed() -> void:
	_show(login_panel)


func _on_login_login_button_pressed() -> void:
	var account_name_edit: LineEdit = $LoginPanel/VBoxContainer/VBoxContainer/VBoxContainer/LineEdit
	var password_edit: LineEdit = $LoginPanel/VBoxContainer/VBoxContainer/VBoxContainer2/LineEdit
	
	var username: String = account_name_edit.text
	var password: String = password_edit.text
	
	var login_button: Button = $LoginPanel/VBoxContainer/VBoxContainer/LoginButton
	login_button.disabled = true
	if (
		CredentialsUtils.validate_username(username).code != CredentialsUtils.UsernameError.OK
		or CredentialsUtils.validate_password(password).code != CredentialsUtils.UsernameError.OK
	):
		login_button.disabled = false
		return

	popup_panel.display_waiting_popup()
	var d: Variant = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.login(),
		{"username": username, "password": password}
	)
	
	# Handle Dictionary response
	if d is Dictionary and (d.has("error") or not d.has("token")):
		if not d.has("error"): d["error"] = "Login failed"
		await popup_panel.confirm_message(str(d))
		login_button.disabled = false
		return
	
	# Store JWT token
	jwt_token = d["token"]
	
	# Fetch worlds
	var worlds_response: Variant = await do_request(
		HTTPClient.Method.METHOD_GET,
		GatewayApi.worlds(),
		{}
	)
	
	if worlds_response is Dictionary and worlds_response.has("error"):
		await popup_panel.confirm_message("Failed to fetch worlds")
		login_button.disabled = false
		return

	fill_connection_info(d["username"], 0) # ID is not critical for display
	populate_worlds(worlds_response)
	
	popup_panel.hide()
	_show($WorldSelection, false)


func _on_guest_button_pressed() -> void:
	popup_panel.display_waiting_popup()

	var d: Variant = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.guest(),
		{GatewayApi.KEY_TOKEN_ID: token}
	)
	if d is Dictionary and d.has("error"):
		await popup_panel.confirm_message(str(d))
		return
	
	# Guest login might need adaptation depending on backend implementation
	# For now assuming similar flow
	jwt_token = d.get("token", "")
	
	var worlds_response: Variant = await do_request(
		HTTPClient.Method.METHOD_GET,
		GatewayApi.worlds(),
		{}
	)
	
	fill_connection_info("Guest", 0)
	populate_worlds(worlds_response)
	
	popup_panel.hide()
	_show($WorldSelection, false)


func _on_world_selected(world_id: int) -> void:
	$WorldSelection.hide()
	popup_panel.display_waiting_popup()
	
	# Note: GET request usually doesn't have body, but passing params for safety if needed
	# The backend expects /api/world/characters (GET) with Authorization header
	var d: Variant = await do_request(
		HTTPClient.Method.METHOD_GET,
		GatewayApi.world_characters(),
		{} 
	)
	
	if d is Dictionary and d.has("error"):
		await popup_panel.confirm_message(str(d))
		$WorldSelection.show()
		return
	
	# d should be an Array of characters
	var characters: Array = []
	if d is Array:
		characters = d
	
	var container: HBoxContainer = $CharacterSelection/VBoxContainer/HBoxContainer
	var i: int = 0
	
	for button: Button in container.get_children():
		if button.pressed.is_connected(_on_character_selected):
			button.pressed.disconnect(_on_character_selected)
			
		if i < characters.size():
			var char_data = characters[i]
			# Backend returns: Id, Name, Level, etc.
			var char_id = char_data["id"] # GUID string or int
			
			button.text = "%s\nLevel: %d" % [
				char_data["name"],
				char_data["level"]
			]
			# Pass character ID (string or int)
			button.pressed.connect(_on_character_selected.bind(world_id, char_id))
		else:
			button.text = "Create New Character"
			# Use empty string or specific marker for new character
			button.pressed.connect(_on_character_selected.bind(world_id, ""))
		i += 1
		
	popup_panel.hide()
	_show($CharacterSelection)


func _on_character_selected(world_id: int, character_id: Variant) -> void:
	current_world_id = world_id
	
	# Check if creating new character (empty string or specific marker)
	if str(character_id) == "":
		_show($CharacterCreation)
		return
	
	$CharacterSelection.hide()
	$BackButton.hide()
	popup_panel.display_waiting_popup()
	
	var d: Variant = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.world_enter(),
		{
			"characterId": character_id
		}
	)
	
	if d is Dictionary and d.has("error"):
		await popup_panel.confirm_message(str(d))
		$CharacterSelection.show()
		$BackButton.show()
		return
	
	# Backend returns: { "token": "...", "host": "...", "port": ... }
	world_server.connect_to_server(d["host"], d["port"], d["token"])
	queue_free.call_deferred()


func _on_create_character_button_pressed() -> void:
	var username_edit: LineEdit = $CharacterCreation/VBoxContainer/VBoxContainer/HBoxContainer2/LineEdit

	var create_button: Button = $CharacterCreation/VBoxContainer/VBoxContainer/CreateButton
	create_button.disabled = true
	$BackButton.hide()
	$CharacterCreation.hide()
	
	var result: Dictionary
	result = CredentialsUtils.validate_username(username_edit.text)
	if result.code != CredentialsUtils.UsernameError.OK:
		await popup_panel.confirm_message("Username:\n" + result.message)
		create_button.disabled = false
		$BackButton.show()
		$CharacterCreation.show()
		return

	popup_panel.display_waiting_popup()
	
	# Create Character
	var d: Variant = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.world_create_char(),
		{
			"name": username_edit.text,
			"skinColor": selected_skin_id, # Mapping simple ID to backend fields
			"hairStyle": 0,
			"hairColor": 0,
			"shirtColor": 0,
			"pantsColor": 0
		}
	)
	
	if d is Dictionary and d.has("error"):
		await popup_panel.confirm_message(str(d))
		create_button.disabled = false
		$CharacterCreation.show()
		return
	
	# Character created successfully (returns CharacterDto)
	# Now enter world with this new character
	var new_char_id = d["id"]
	
	var enter_response: Variant = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.world_enter(),
		{
			"characterId": new_char_id
		}
	)
	
	if enter_response is Dictionary and enter_response.has("error"):
		await popup_panel.confirm_message("Character created but failed to enter: " + str(enter_response))
		create_button.disabled = false
		$CharacterCreation.show()
		return

	world_server.connect_to_server(
		enter_response["host"],
		enter_response["port"],
		enter_response["token"]
	)
	queue_free.call_deferred()


func create_account() -> void:
	var name_edit: LineEdit = $CreateAccountPanel/VBoxContainer/VBoxContainer/VBoxContainer/LineEdit
	var password_edit: LineEdit = $CreateAccountPanel/VBoxContainer/VBoxContainer/VBoxContainer2/LineEdit
	var password_repeat_edit: LineEdit = $CreateAccountPanel/VBoxContainer/VBoxContainer/VBoxContainer3/LineEdit

	if password_edit.text != password_repeat_edit.text:
		await popup_panel.confirm_message("Passwords don't match")
		return
	
	var result: Dictionary
	result = CredentialsUtils.validate_username(name_edit.text)
	if result.code != CredentialsUtils.UsernameError.OK:
		await popup_panel.confirm_message("Username:\n" + result.message)
		return
	result = CredentialsUtils.validate_password(password_edit.text)
	if result.code != CredentialsUtils.UsernameError.OK:
		await popup_panel.confirm_message("Password:\n" + result.message)
		return
	
	$CreateAccountPanel.hide()
	popup_panel.display_waiting_popup()

	var d: Dictionary = await do_request(
		HTTPClient.Method.METHOD_POST,
		GatewayApi.account_create(),
		{"username": name_edit.text, "password": password_edit.text}
	)
	if d.has("error") or not d.has("token"):
		if not d.has("error"): d["error"] = "Registration failed"
		await popup_panel.confirm_message(str(d))
		$CreateAccountPanel.show()
		return
	
	# New backend response: { "userId": 1, "username": "name", "token": "jwt...", "expiration": "..." }
	fill_connection_info(d["username"], d["userId"])
	populate_worlds({}) # TODO: Fetch worlds
	
	jwt_token = d["token"]

	popup_panel.hide()
	_show($WorldSelection, false)


func _on_create_account_button_pressed() -> void:
	_show($CreateAccountPanel)


func populate_worlds(world_info: Variant) -> void:
	var container: HBoxContainer = $WorldSelection/VBoxContainer/HBoxContainer
	
	var i: int = 0
	for button: Button in container.get_children():
		if button.pressed.is_connected(_on_world_selected):
			button.pressed.disconnect(_on_world_selected)
			
		if world_info is Array and i < world_info.size():
			var world = world_info[i]
			# Backend returns: Id, Name, Address, Port, CurrentPlayers, MaxPlayers, Status
			button.text = "%s\n\n%s" % [
				world.get("name", "Unknown"),
				world.get("status", "Online")
			]
			# Assuming world ID is string in backend but int in Godot logic? 
			# If backend uses string IDs (e.g. "world-1"), we might need to adapt.
			# For now, passing 0 or parsing if possible.
			button.pressed.connect(_on_world_selected.bind(0)) 
			
		elif world_info is Dictionary and i < world_info.size():
			# Fallback for old format if needed
			var world_id: String = world_info.keys()[i]
			button.text = "%s\n\n%s" % [
				world_info[world_id].get("name", "name"),
				" \n".join(str(world_info[world_id]["info"]).split(", "))
			]
			button.pressed.connect(_on_world_selected.bind(world_id.to_int()))
			
		else:
			button.hide()
		i += 1


func fill_connection_info(_account_name: String, _account_id: int) -> void:
	account_name = _account_name
	account_id = _account_id
	$ConnectionInfo.text = "Account-name: %s\nAccount-ID: %s" % [
		account_name, account_id
	]
