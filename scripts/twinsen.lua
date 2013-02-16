local Behavior = {
	Normal = 0,
	Athletic = 1,
	Aggressive = 2,
	Discreet = 3
}

local AnimationMap = {
	idle = { [Behavior.Normal]="idle", [Behavior.Athletic]="idle_athletic", [Behavior.Aggressive]="idle_agressive", [Behavior.Discreet]="idle_discreet" },
	walk = { [Behavior.Normal]="walk_loop", [Behavior.Athletic]="run_loop" },
	walkback = { [Behavior.Normal]="walk_backward_loop", [Behavior.Athletic]="run_backward_loop" },
	left = { [Behavior.Normal]="rotate_left" },
	right = { [Behavior.Normal]="rotate_right" },
	jump = { [Behavior.Normal]="jump" },
	climb = { [Behavior.Normal]="climb_up_loop" },
	fall = { [Behavior.Normal]="fall_loop" }
}

Entity.behavior = Behavior.Normal
Entity.velocity = 0
Entity.angularVel = 0
Entity.state = "Grounded"
Entity.oldState = Entity.state
Entity.animationSpeed = 1.0;

function Entity:init()
	Input:Bind(Input.Keys.F1, function(down) if down then self:changeBehavior(Behavior.Normal) end end)
	Input:Bind(Input.Keys.F2, function(down) if down then self:changeBehavior(Behavior.Athletic) end end)
	Input:Bind(Input.Keys.F3, function(down) if down then self:changeBehavior(Behavior.Aggressive) end end)
	Input:Bind(Input.Keys.F4, function(down) if down then self:changeBehavior(Behavior.Discreet) end end)
	Input:Bind(Input.Keys.Up, function(down) self:updateVel(down) end)
	Input:Bind(Input.Keys.Down, function(down) self:updateVel(not down) end)
	Input:Bind(Input.Keys.Left, function(down) self:updateAngVel(down) end)
	Input:Bind(Input.Keys.Right, function(down) self:updateAngVel(not down) end)
	Input:Bind(Input.Keys.Space, function(down) if down then self.motion:Jump() end end)

	self.animation = self:GetComponent("BlendAnimationController")
	self.motion = self:GetComponent("MotionComponent")
	self.motion.StateChanged:add(function(sender, e) self.state = e.Current:ToString(); self.oldState = e.Previous:ToString(); self:updateAnimation() end)
	self:GetComponent("HealthComponent").Hit:add(function(sender, e) self:onDamaged() end)
	self:updateAnimation()
end

function Entity:updateVel(down)
	if down then self.velocity = self.velocity + 1 else self.velocity = self.velocity - 1 end
	self:updateControl()
end

function Entity:updateAngVel(down)
	if down then self.angularVel = self.angularVel + 1 else self.angularVel = self.angularVel - 1 end
	self:updateControl()
end

function Entity:updateControl()
	self.motion:SetAngularVelocity(self.angularVel)
	self.motion:SetTargetVelocity(self.velocity)
	self:updateAnimation()
end

function Entity:changeBehavior(mode)
	self.behavior = mode
	self:updateAnimation()
end

function Entity:updateAnimation()
	local newAnim = nil
	local speed = 1.0 -- animation speed factor
	if self.state == "Grounded" then
		if self.velocity == 0 then
			if self.angularVel == 0 then
				newAnim = self:getAnimation("idle")
			elseif self.angularVel > 0 then
				newAnim = self:getAnimation("left")
			elseif self.angularVel < 0 then
				newAnim = self:getAnimation("right")
			end
		elseif self.velocity > 0 then
			newAnim = self:getAnimation("walk")
		elseif self.velocity < 0 then
			newAnim = self:getAnimation("walkback")
		end
	elseif self.state == "Jumping" then
		newAnim = self:getAnimation("jump")
	elseif self.state == "Falling" then
		newAnim = self:getAnimation("fall")
	elseif self.state == "Climbing" then
		newAnim = self:getAnimation("climb")
		speed = self.velocity
	else
		-- safety fallback, in case of undefined behavior
		newAnim = self:getAnimation("idle")
	end
	if newAnim ~= self.currentAnim then
		self.currentAnim = newAnim
		self.animation:Crossfade(newAnim, 0.5, true)
	end
	if speed ~= self.animation.Speed then
		self.animation.Speed = speed	
	end
end

function Entity:getAnimation(name)
	if AnimationMap[name][self.behavior] == nil then
		return AnimationMap[name][Behavior.Normal]
	else
		return AnimationMap[name][self.behavior]
	end
end

function Entity:onDamaged()
	print "UGH!"
end