function Entity:init()
	self:getComponent("LuaComponent").UseDeprecatedAPI = true
end

function Entity:update()
	local current_anim=get_anim(this)
	local current_frame=get_frame(this)
	local anim_finished=is_anim_finished(this)
	local is_wounded=get_wounded(this)
	local d=distance(this,"heroe")
	local a=angle(this,"heroe")
	local rot_direction=0.
	local rot_step=0.
	local vertical_step=0.
	local gravity=10
	local step=0

	local right=false
	local left=false
	local chase=false
	local fight=false
	local shoot=false
	if distance(this,"heroe")<2 then
		fight=true
	elseif distance(this,"heroe")<4 then
		shoot=true
	elseif distance(this,"heroe")<7 then
		chase=true
	end
		
	if chase==true or fight==true or shoot==true then
		if a>0 then
			left=true
		elseif a<0 then
			right=true
		end
	end
	
	if current_anim~="hited" and collision_between(this,"heroe")==true then
		if get_anim("heroe")=="kick" and get_frame("heroe")>15 then
			is_wounded=true
		elseif get_anim("heroe")=="right_punch" and get_frame("heroe")>15 then
			is_wounded=true
		elseif get_anim("heroe")=="left_punch" and get_frame("heroe")>15 then
			is_wounded=true
		end
	end
		
	if current_anim=="hited" and anim_finished==false then
		next_anim="hited"
	elseif is_wounded==true then
		next_anim="hited"
	elseif current_anim=="attack_hit" and anim_finished==false then
		next_anim="attack_hit"
	elseif current_anim=="attack_shoot" and anim_finished==false then
		next_anim="attack_shoot"
	elseif fight==true then
		if get_anim("heroe")~="hited_harder" then
			next_anim="attack_hit"
		else
			next_anim="idle_aggressive"
		end
	elseif chase==true then
		if current_anim=="walk" 
		or (current_anim=="walk_start" and anim_finished==true) then
			next_anim="walk"
		else 
			next_anim="walk_start"
		end
	elseif shoot==true then
			next_anim="attack_shoot"
	else
		next_anim="idle_athletic"
	end
		
	if next_anim~=current_anim then
		set_anim(this,next_anim)
	end

	if current_anim=="walk_start" then
		step=2.5
		rot_step=200
	elseif current_anim=="walk" then
		x=get_frame_ratio(this)
		step=(2.9+0.25*math.cos(x*math.pi*4+2.9))
		rot_step=200
		if current_frame==8 then
			play_sound(this,"assets/sounds/137 walk stone 1.wav")
		elseif current_frame==36 then
			play_sound(this,"assets/sounds/152 walk stone 2.wav")
		end
	elseif current_anim=="attack_hit" then
		rot_step=60
		step=1
		if current_frame==18 then 
			play_sound_random_pitch(this,"assets/sounds/096 swing 1.wav")
		end
		if get_anim("heroe")~="hited" and collision_between(this,"heroe")==true then
		if current_frame>20 then
			set_wounded("heroe",true)
		end
	end
		
	elseif current_anim=="hited" then
		step=-1.
		rot_step=30
		if current_frame==1 then
			set_wounded(this,false)
			play_sound_random_pitch(this,"assets/sounds/003 quetch grumble.wav")
			set_health(this,get_health(this)-1)
			hit_particles_name=create_valid_object_name("hit_particles")
			append_object("assets/hit_particles.map","hit_particles.000",hit_particles_name)
			wait(hit_particles_name,30)
			set_pos(hit_particles_name,
			get_pos_x(this),
			get_pos_y(this),
			get_pos_z(this)+2)
			
		end
	elseif current_anim=="attack_shoot" then
		if current_frame==60 then
			play_sound_random_pitch(this,"assets/sounds/shoot.wav")
			horiz_angle=(get_rot_z(this)+90)*math.pi/180.
			vert_angle=10.*math.pi/180.
			speed=0.4
			delta=5.*math.pi/180.
			for i=-2,2 do
				bullet_name=create_valid_object_name("soldier_bullet")
				append_object("assets/projectiles.map","soldier_bullet.000",bullet_name)
				wait(bullet_name,20)
				set_speed(bullet_name,
				speed*math.cos(horiz_angle+i*delta)*math.cos(vert_angle),
				speed*math.sin(horiz_angle+i*delta)*math.cos(vert_angle),
				speed*math.sin(vert_angle))
				set_pos(bullet_name,
				get_pos_x(this)+1.5*math.cos(horiz_angle),
				get_pos_y(this)+1.5*math.sin(horiz_angle),
				get_pos_z(this)+2)
			end
		end
		rot_step=60
		step=0
	end

	rot_step=rot_step/60.
	step=step/60.
	vertical_step=vertical_step/60.

	if right==true then
		rot_step=-rot_step
	elseif left==true then
		rot_step=rot_step
	else
		rot_step=0
	end

	rotate_step(this,0.,0.,rot_step)
	set_gravity(this,gravity)
	move_step_local(this,0,step,vertical_step)
	--set_anim(this,"idle")
	if get_health(this)<=0 then
		remove_object(this)
	end
end