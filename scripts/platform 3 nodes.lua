if is_at_node(this)==true then
	if get_node(this)==0 then
		if get_sound(this)=="assets/sounds/HorPlat_Loop.ogg" then
			play_sound(this,"assets/sounds/HorPlat_End.ogg")
		end
		if distance(this,"heroe")<1 and key_space()==true then
			play_sound(this,"assets/sounds/HorPlat_Start.ogg")
			play_sound_loop(this,"assets/sounds/HorPlat_Loop.ogg")
			wait(this,0)
			set_node(this,1)
		end
	elseif get_node(this)==1 then
		if get_sound(this)=="assets/sounds/HorPlat_Loop.ogg" then
			play_sound(this,"assets/sounds/HorPlat_End.ogg")
		end
		if distance(this,"heroe")<1 and key_space()==true then
			play_sound(this,"assets/sounds/HorPlat_Start.ogg")
			play_sound_loop(this,"assets/sounds/HorPlat_Loop.ogg")
			wait(this,0)
			set_node(this,2)
		end
	elseif get_node(this)==2 then
		if get_sound(this)=="assets/sounds/HorPlat_Loop.ogg" then
			play_sound(this,"assets/sounds/HorPlat_End.ogg")
		end
		if distance(this,"heroe")<1 and key_space()==true then
			play_sound(this,"assets/sounds/HorPlat_Start.ogg")
			play_sound_loop(this,"assets/sounds/HorPlat_Loop.ogg")
			wait(this,0)
			set_node(this,3)
		end
	elseif get_node(this)==3 then
		if get_sound(this)=="assets/sounds/HorPlat_Loop.ogg" then
			play_sound(this,"assets/sounds/HorPlat_End.ogg")
		end
		if distance(this,"heroe")<1 and key_space()==true then
			play_sound(this,"assets/sounds/HorPlat_Start.ogg")
			play_sound_loop(this,"assets/sounds/HorPlat_Loop.ogg")
			wait(this,0)
			set_node(this,0)
		end
	end
end

if has_waited(this)==true then
	move_to_node()
end
