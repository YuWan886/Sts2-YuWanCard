extends ColorRect

var elapsed_time: float = 0.0
var shader_material: ShaderMaterial
var duration: float = 2.0
var fade_in_duration: float = 0.2
var fade_out_duration: float = 0.4

func _ready():
	shader_material = material as ShaderMaterial
	if shader_material == null:
		push_error("VfxMatrixRainController: ShaderMaterial not found")
		queue_free()
		return
	
	shader_material.set_shader_parameter("time", 0.0)
	shader_material.set_shader_parameter("intensity", 1.0)
	shader_material.set_shader_parameter("fade", 0.0)
	
	var timer = get_node_or_null("Timer")
	if timer:
		duration = timer.wait_time

func _process(delta: float):
	if shader_material == null:
		return
	
	elapsed_time += delta
	shader_material.set_shader_parameter("time", elapsed_time)
	
	var fade: float = 1.0
	
	if elapsed_time < fade_in_duration:
		fade = elapsed_time / fade_in_duration
		fade = ease(fade, -2.0)
	elif elapsed_time > duration - fade_out_duration:
		fade = (duration - elapsed_time) / fade_out_duration
		fade = ease(fade, 2.0)
	
	fade = clamp(fade, 0.0, 1.0)
	shader_material.set_shader_parameter("fade", fade)

func set_duration(new_duration: float):
	duration = new_duration
	var timer = get_node_or_null("Timer")
	if timer:
		timer.wait_time = new_duration
