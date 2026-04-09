extends ColorRect

const TOTAL_FRAMES: int = 48
const FPS: float = 16.0
const FADE_IN_DURATION: float = 0.1
const FADE_OUT_START: float = 1.5
const TOTAL_DURATION: float = 2.0

var elapsed_time: float = 0.0
var frames: Array[Texture2D] = []
var current_frame: int = 0
var sprite: Sprite2D

func _ready():
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	anchors_preset = Control.PRESET_FULL_RECT
	anchor_right = 1.0
	anchor_bottom = 1.0
	color = Color(0.0, 0.0, 0.0, 0.5)
	
	_load_frames()
	_setup_sprite()
	
func _load_frames():
	for i in range(1, TOTAL_FRAMES + 1):
		var frame_path = "res://YuWanCard/images/vfx/blood_wheel_eye/blood_wheel_eye_%d.png" % i
		var texture = load(frame_path) as Texture2D
		if texture:
			frames.append(texture)
		else:
			push_warning("VfxBloodWheelEye: Failed to load frame: %s" % frame_path)
	
	if frames.is_empty():
		push_error("VfxBloodWheelEye: No frames loaded!")
		queue_free()
		return

func _setup_sprite():
	sprite = Sprite2D.new()
	sprite.centered = true
	sprite.z_index = 100
	add_child(sprite)
	
	var viewport_size = get_viewport().get_visible_rect().size
	sprite.position = viewport_size * 0.5
	
	if frames.size() > 0:
		var first_frame = frames[0]
		var frame_size = first_frame.get_size()
		var scale_x = viewport_size.x / frame_size.x
		var scale_y = viewport_size.y / frame_size.y
		var sprite_scale = max(scale_x, scale_y) * 0.8
		sprite.scale = Vector2(sprite_scale, sprite_scale)

func _process(delta: float):
	elapsed_time += delta
	
	if elapsed_time >= TOTAL_DURATION:
		queue_free()
		return
	
	_update_frame()
	_update_fade()

func _update_frame():
	if frames.is_empty() or sprite == null:
		return
	
	var frame_index = int(elapsed_time * FPS) % frames.size()
	if frame_index != current_frame:
		current_frame = frame_index
		sprite.texture = frames[current_frame]

func _update_fade():
	if elapsed_time < FADE_IN_DURATION:
		modulate.a = elapsed_time / FADE_IN_DURATION
	elif elapsed_time > FADE_OUT_START:
		var fade_progress = (elapsed_time - FADE_OUT_START) / (TOTAL_DURATION - FADE_OUT_START)
		modulate.a = 1.0 - fade_progress
	else:
		modulate.a = 1.0
