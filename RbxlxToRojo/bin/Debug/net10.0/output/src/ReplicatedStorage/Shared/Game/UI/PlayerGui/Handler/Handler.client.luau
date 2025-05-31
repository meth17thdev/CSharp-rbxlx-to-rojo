local TweenService = game:GetService("TweenService")
local UserInputService = game:GetService("UserInputService")

local canvas: CanvasGroup = script.Parent

local canAct = true

local tweenInfo = TweenInfo.new(0.5, Enum.EasingStyle.Quad, Enum.EasingDirection.Out)
local openTween = TweenService:Create(canvas, tweenInfo, { GroupTransparency = 1 })
local closeTween = TweenService:Create(canvas, tweenInfo, { GroupTransparency = 0 })

UserInputService.InputBegan:Connect(function(input: InputObject, a1: boolean)
	local keyCode = input.KeyCode

	if keyCode == Enum.KeyCode.Tab and canAct then
		canAct = false
		task.delay(0.55, function()
			canAct = true
		end)

		local transparency = canvas.GroupTransparency

		if transparency == 1 then
			closeTween:Play()
		elseif transparency == 0 then
			openTween:Play()
		end
	end
end)
