function Entity:init()
	self:GetComponent("ProjectileComponent").EntityHit:add(function(sender, e) self:onHit(e.Other.Name) end)
end

function Entity:onHit(name)
	local other = GetEntity(name)
	local health = other:GetComponent("HealthComponent")
	if health ~= nil then -- hit something damageable
		health:DoDamage(0)
	end
	self:Remove();
end