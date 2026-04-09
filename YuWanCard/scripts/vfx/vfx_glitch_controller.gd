extends ColorRect

var elapsed_time: float = 0.0
var shader_material: ShaderMaterial

func _ready():
	shader_material = material as ShaderMaterial
	if shader_material == null:
		push_error("VfxGlitchController: ShaderMaterial not found")
		queue_free()
		return
	
	shader_material.set_shader_parameter("glitch_intensity", 0.0)
	shader_material.set_shader_parameter("time", 0.0)

func _process(delta: float):
	if shader_material == null:
		return
	
	elapsed_time += delta
	shader_material.set_shader_parameter("time", elapsed_time)
