-- Edit me while the demo is running — the output changes on the next tick.
-- (no os/io/package here: this runs in the hardened sandbox)
local M = {}

function M.describe(p)
    local mood = "calm"
    if p.hp_fraction < 0.5 then mood = "frenzied" end
    return mood .. " @ " .. string.format("%.1f", p.distance) .. "m"
end

return M
