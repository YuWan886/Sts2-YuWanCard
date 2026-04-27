extends SpineSprite

@export var initial_skin: String = "normal"

func _ready() -> void:
    var skeleton = get_skeleton()
    if skeleton == null:
        return
    var data = skeleton.get_data()
    if data == null:
        return
    var skin = data.find_skin(initial_skin)
    if skin != null:
        skeleton.set_skin(skin)
        skeleton.set_slots_to_setup_pose()
