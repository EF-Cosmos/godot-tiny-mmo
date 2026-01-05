# Start Forest Server
Write-Host "Starting Forest Server on Port 8087..."
Start-Process "godot" -ArgumentList "--headless", "--path", ".", "--", "--mode=world-server", "--config=res://data/config/world_config_forest.cfg" -NoNewWindow

# Start Desert Server
Write-Host "Starting Desert Server on Port 8088..."
Start-Process "godot" -ArgumentList "--headless", "--path", ".", "--", "--mode=world-server", "--config=res://data/config/world_config_desert.cfg" -NoNewWindow

Write-Host "Servers started. Press Ctrl+C to stop (if running in foreground) or kill processes manually."
