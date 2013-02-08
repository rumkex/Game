push=false

if can_hit_wall("heroe") and distance("heroe",this)<3 and math.abs(angle("heroe",this))<10 then
	set_can_push("heroe",true)
	if get_anim("heroe")=="push_loop" then
		push=true
	end
end


--push=distance("heroe",this)<3 and get_anglenim("heroe")=="push_loop"

a=get_rot_z("heroe")+90
a=math.fmod(a,360)
a=a+360
a=math.fmod(a,360)

if push==true then
	x=0.
	y=0.
	if a>315 or a <45 then
		x=0.025
	elseif a>45 and a<135 then
		y=0.025
	elseif a>135 and a<225 then
		x=-0.025
	elseif a>225 and a<315 then
		y=-0.025
	end
	--log(tostring(move_step_collision_test(this,x,y,0)))
	move_step(this,x,y,0)
--	if move_step_collision_test(this,x,y,0)==false then
--		move_step(this,x,y,0.)
--	end
end
