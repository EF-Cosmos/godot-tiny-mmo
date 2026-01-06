启动多个地图服务器：
  # 主世界
  godot --headless source/server/world/world_main.tscn

  # 森林实例
  godot --headless source/server/world/world_main.tscn --config=data/config/world_config_forest.cfg

  # 副本
  godot --headless source/server/world/world_main.tscn --config=data/config/world_config_dungeon.cfg

  查询服务器：
  curl "http://localhost:5003/api/rooms?mapName=Forest"

