local button = script.Parent

button.Touched:Connect(function(part)
	local character = part.Parent

	if character:IsA("Model") then
		local humanoid = character:FindFirstChildOfClass("Humanoid")

		if humanoid then
		end
	end
end)
