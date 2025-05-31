local isInitialized: BoolValue = script:WaitForChild("IsInitialized")
isInitialized.Value = false

print("initializing player...")

isInitialized.Value = true

print("player is initialized!")
