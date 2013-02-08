owner="heroe"
--play_sound(this,"assets/sounds/bouncing.wav");
if has_waited(this)==true then
--[[	if math.abs(get_pos_x(this)-get_pos_x(owner))>1 or
	math.abs(get_pos_y(this)-get_pos_y(owner))>1 or
	math.abs(get_pos_z(this)-get_pos_z(owner))>1 then
		dx=get_pos_x(this)-get_pos_x(owner)
		dy=get_pos_y(this)-get_pos_y(owner)
		dz=get_pos_z(this)-get_pos_z(owner)
		L=math.sqrt(dx*dx+dy*dy+dz*dz)
		speed=0.3
		if L<1.5 then 
			remove_object(this)
		else
			dx=-speed*dx/L
			dy=-speed*dy/L
			dz=-speed*dz/L
			move_step(this,dx,dy,dz)
		end
	end]]
	remove_object(this)
end