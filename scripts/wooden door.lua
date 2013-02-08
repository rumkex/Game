if is_at_node(this)==true then
    if get_node(this)==0 and distance(this,"heroe")<2 and key_space()==true then
        set_node(this,1)
        play_sound(this,"assets/sounds/door_wooden_slide.wav")
    elseif get_node(this)==1 and distance(this,"heroe")>3 then
        set_node(this,0)
        play_sound(this,"assets/sounds/door_wooden_slide.wav")
    end
end

move_to_node()
