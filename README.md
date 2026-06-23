# g-2dbh

# TODO
## bugs
- no way of setting author in custom level

## complaints
- laggy
- add multi select
- path render fidelity is so low its actually bad (also add maximum path length)
- maybe add "increment by x time on place"?
- make t slider on previews max out at projectile / pattern duration [COMPLETED_FOR_PROJECTILES]
- add grid snapping
- no visual difference between models and projectiles in library
- placement and selection is weird (just make it like geometry dash's)
- ADD UNDO / REDO
- add texture scale slider to projectile creator
- add refresh texture cache button + automatically refresh texture cache when tabbing into game
- add no collide button to projectiles
- add preview width and height display
- no way of knowing what variables to use or where
- add open level folder from custom level select

## potential optimizations
- calculate delta instead of directly calculating position, allowing for caching results (projectile.calculatePosition)
- add dirty flag to preview