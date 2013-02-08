local Behavior = {
	Normal = 0,
	Athletic = 1,
	Aggressive = 2,
	Discrete = 3
}

Entity.behavior = Behavior.Normal

function Entity:init()
	Input:Bind(Input.Keys.F1, function() self:changeBehavior(Behavior.Normal) end)
	Input:Bind(Input.Keys.F2, function() self:changeBehavior(Behavior.Athletic) end)
	Input:Bind(Input.Keys.F3, function() self:changeBehavior(Behavior.Aggressive) end)
	Input:Bind(Input.Keys.F4, function() self:changeBehavior(Behavior.Discrete) end)

	self.animation = self:GetComponent("BlendAnimationController")
	self:GetComponent("MotionComponent").StateChanged:add(function(sender, e) self:motionStateChanged(e.Current:ToString(), e.Previous:ToString()) end)
	self:GetComponent("HealthComponent").Hit:add(function(sender, e) self:onDamaged() end)
	self.animation:Start("idle", true)
end

function Entity:changeBehavior(mode)
	print(mode)
end

function Entity:motionStateChanged(state, oldState)
	--if (state == "Grounded")
end

function Entity:onDamaged()

end