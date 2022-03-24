module AnarchyCollab2022SimplifiedGraphicsController

using ..Ahorn, Maple

@mapdef Entity "AnarchyCollab2022/SimplifiedGraphicsController" SimplifiedGraphicsController(
  x::Integer, y::Integer,
  solids_color::String="ff7f50",
  danger_color::String="ff0000",
  danger_inactive_color::String="8b0000",
  interactive_color::String="9acd32",
  render_virtually::Bool=false,
  force_opaque::Bool=false#,
#   suppress_distort::Bool=false
)

@mapdef Entity "AnarchyCollab2022/SimplifiedGraphicsWindow" SimplifiedGraphicsWindow(
  x::Integer, y::Integer,
  width::Integer=16, height::Integer=16,
)

const placements = Ahorn.PlacementDict(
    "Simplified Graphics Controller (AnarchyCollab2022)" => Ahorn.EntityPlacement(
        SimplifiedGraphicsController,
    ),
    "Simplified Graphics Window (AnarchyCollab2022)" => Ahorn.EntityPlacement(
        SimplifiedGraphicsWindow,
        "rectangle"
    ),
)

function Ahorn.selection(entity::SimplifiedGraphicsController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

Ahorn.minimumSize(entity::SimplifiedGraphicsWindow) = 8, 8
Ahorn.resizable(entity::SimplifiedGraphicsWindow) = true, true
Ahorn.selection(entity::SimplifiedGraphicsWindow) = Ahorn.getEntityRectangle(entity)

# TODO: better image
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SimplifiedGraphicsController, room::Maple.Room) = Ahorn.drawImage(ctx, Ahorn.Assets.northernLights, -12, -12)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SimplifiedGraphicsWindow, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.0, 0.0, 0.0, 0.3), (0.6, 0.8, 0.2, 1.0))
end

end