local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local ServerStorage = game:GetService("ServerStorage")
local PlayerData = require(ReplicatedStorage.Shared.Game.Data.PlayerData)
local ProfileStore = require(ServerStorage.Packages.ProfileStore)

local getPlayerDataEvent: RemoteEvent = ReplicatedStorage.Shared.Remotes.GetPlayerDataEvent

local PlayerDataStore = ProfileStore.New("PlayerDataStore", PlayerData.template())

local Profiles = {}

-------> anonymous functions replacements

local function StartSession(player: Player)
	local profile = PlayerDataStore:StartSessionAsync(tostring(player.UserId), {
		Cancel = function()
			return player.Parent ~= Players
		end,
	})

	if profile ~= nil then
		profile:AddUserId(player.UserId)
		profile:Reconcile()

		profile.OnSessionEnd:Connect(function()
			Profiles[player.UserId] = nil
			player:Kick(`Profile session ended - Please rejoin`)
		end)

		if player.Parent == Players then
			print(`Profile loaded for {player.Name}!`)
			Profiles[player.UserId] = profile
		else
			profile:EndSession()
		end
	else
		player:Kick(`Profile load fail - Please rejoin`)
	end
end

local function OnPlayerAdded(player: Player)
	StartSession(player)
end

local function OnPlayerRemoved(player: Player)
	local profile = Profiles[player]
	if profile ~= nil then
		profile:EndSession()
	end
end

local function OnPlayerDataRequest(player: Player)
	local profile = Profiles[player.UserId]
	if profile then
		getPlayerDataEvent:FireClient(player, profile.Data)
	else
		warn("Player data not found for:", player.Name)
		StartSession(player)

		task.spawn(function()
			while task.wait(0.1) do
				profile = Profiles[player.UserId]
				if profile then
					break
				end
			end

			getPlayerDataEvent:FireClient(player, profile.Data)
		end)
	end
end

-------> events

getPlayerDataEvent.OnServerEvent:Connect(OnPlayerDataRequest)

Players.PlayerAdded:Connect(OnPlayerAdded)

Players.PlayerRemoving:Connect(OnPlayerRemoved)

local function DeserializeData(data, parent)
	for key, value in pairs(data) do
		if type(value) == "table" then
			local folder = Instance.new("Folder")
			folder.Name = key
			folder.Parent = parent
			DeserializeData(value, folder)
		elseif type(value) == "string" then
			local valueObj = Instance.new("StringValue")
			valueObj.Name = key
			valueObj.Value = value
			valueObj.Parent = parent
		elseif type(value) == "number" then
			local valueObj = Instance.new("NumberValue")
			valueObj.Name = key
			valueObj.Value = value
			valueObj.Parent = parent
		elseif type(value) == "boolean" then
			local valueObj = Instance.new("BoolValue")
			valueObj.Name = key
			valueObj.Value = value
			valueObj.Parent = parent
		end
	end
end
