# Game Board visual refresh

## Assets and generation

Generated with the built-in image_gen tool, using the three user-provided references. Final assets live in `TrailofEvolution/Assets/Resources/Board/`:

- `EvolutionMap.png`: full-board colored-pencil / watercolor map, with original winding route, rain, Carles Darwin, snake, crab, cabins, fence, ponds, stick-figure evolution, shortcut, bird evolution and walking shark.
- `CardAtlas.png`: sixteen art cells, seven mutation faces, seven scenario faces, two card backs. Runtime UV windows select cells without duplicating textures. Card titles and rules are real UI text.

Generation prompt, map: Production game board texture, landscape 3:2. Reference game_map.jpg defines layout and whimsical details; tabletop photograph defines depth and texture. Overhead map with hand-painted colored pencil and watercolor, relief shadows, no blur. Alternating emerald and earthy brown winding route from bottom-left START through two hairpins to upper-right END. Yellow jagged explosion upper left with hollow outlined “Trail of Evolution”. Preserve rain and “it's raining”, red-car-headed “Carles Darwin”, snake, crab, cabins, fence, evergreen forest, two ponds and crocodile, top Homosa → Homosapiensis stick figures, lower-middle red shortcut bridge with “Shortcut (costs 2 random negative events)”, right-side Geoge → Wember → Penguin → Eagle → Evolved Poodle, bottom-right shark → shark with legs. Diverse trees in empty spaces. No UI, tokens or depth-of-field blur.

Generation prompt, cards: Production atlas, exactly four columns by four rows. Match supplied Carnivore reference: organic dark twisted-wood frame, ivory parchment header and description, orange-coral watercolor edge, luminous painted animals, green corner badge, red wax seal. Blank text areas for runtime labels. Row 1: peacock, chameleon, running hare, tortoise. Row 2: giraffe, tiger claws, large-beaked finch, running rabbits. Row 3: earthquake, drought, eagle, hawk. Row 4: white tiger predator, hard seeds and finch, amber tree-of-life card back, teal/gold evolution spiral card back. No generated letters or numbers.

## Runtime presentation

`TabletopBoard.cs` builds a thick wooden board, illustrated top surface and resin animal miniatures as actual Unity mesh objects. A dedicated perspective camera (25 degrees from the board normal) and RenderTexture embed the scene in the responsive UI. No postprocessing or focus blur. Deer, fox and turtle are selected using the startup avatar index; mint, coral and blue match startup colors. The CPU uses the next animal and its gold player color. Materials are retained as Resources assets for standalone shader inclusion; generated meshes, cameras, textures and materials are cleaned up on scene changes.

`PaintedCards.cs` gives all card faces the reference portrait ratio, wood frame and serif typography. Full scenario wording remains in GameRules.cs. Two full-size hand cards sit at the bottom of the right sidebar without duplicate trait summaries. Drawing first adds a third card; the user clicks one to discard. Three-card hands stagger the middle card upward to keep its title reachable when a neighboring card is raised. During three-card discard selection only, hover highlights and lifts cards with unscaled-time animation. Two-card hands have neither hover behavior nor click actions. Outside discard selection, hand-card clicks do nothing. The previous footer is removed. Scenario cards carry their own Apply Event button directly below the face.

The 45 logical movement positions are remapped onto the illustrated route; no new game rules are introduced. The shortcut still uses the existing 18 → 31 rule until its exact traversal is agreed. Printed path divisions are decorative, not individually numbered logical positions.

## Validation

See `TrailofEvolution/Logs/board-build.log` and `board-smoke.log` for local build and integration results. The smoke run captures the main board, scenario card, deer and turtle variants and completes three games with alternating first player. These automated captures do not replace user acceptance of the artwork.

The smoke-test deadline is 180 seconds for three games: the previous 90-second limit was exceeded by a random long match with no runtime exception. The hidden-window capture explicitly renders the board camera before rendering the overlay canvas, so a missing tabletop cannot be mistaken for a valid UI screenshot.

Final verification: Unity 6000.3.10f1 Windows Development Build passed (`TRAIL_BUILD_OK`); rule validation passed (`TRAIL_RULES_OK`); the final installed executable completed three games with both starting orders (`TRAIL_SMOKE_OK`, exit 0). Board, scenario, deer and turtle screenshots are saved in `docs/previews/`.


Two-card rules: both held traits apply, with any matching beneficial trait protecting the player and any matching harmful trait still triggering its penalty. The AI evaluates the combined risk of each candidate pair; it can discard either old card or the new card. Eight physical mutations are conserved across both hands and the draw/discard piles. Rule validation covers 500 draw-three/discard-one cycles. Interaction smoke testing clicks actual hand and event buttons and captures ThreeCardHand, HandHover and ApplyEvent.

Hand-interaction refresh verified: `hand-build.log` records successful rules and Windows build; `hand-smoke.log` records three completed games and exit 0, with real discard-button callbacks, hover lift assertions, and Apply Event clicks. Updated screenshots: `GameBoardScene.png`, `ThreeCardHand.png`, `HandHover.png`, and `ApplyEvent.png` in `docs/previews/`.


## Transparent cards and wider map

The right sidebar is now 364 units wide (previously 520); hand cards are 154 units wide (previously 220). Both shrink by 30%. No mutation/event counts are displayed. The board viewport grows from 1010 × 800 to 1460 × 1110 and extends behind the sidebar. Camera background matches the full-screen background to remove the separate green rectangle. A dedicated prompt directly above the hand displays the exact text “Select one card to discard” only while choosing a discard. Clicking either card in a two-card hand does nothing.

Asset: `TrailofEvolution/Assets/Resources/Board/CardAtlas.png` was edited with the built-in image_gen tool. Verified RGBA alpha range 0–255; Unity alpha transparency import enabled.

Edit prompt: Precise background extraction edit for an existing game texture atlas. Remove ONLY the orange/coral/pink background OUTSIDE the brown wooden frames of ALL SIXTEEN cards, replacing it with actual alpha transparency. Keep brown wooden frames, green medallions, red wax seals, parchment, all illustrations and both ornate card backs opaque and unchanged. Preserve the exact four-column/four-row atlas layout, cell order, framing and registration. Every card stays in its original cell. No new art, no text, no layout shifts. Transparent spaces between cards and outside wooden silhouettes; no orange halos or baked checkerboard.

Final layout verification: `layout-build.log` contains TRAIL_BUILD_OK and rule validation success; `layout-smoke.log` contains TRAIL_SMOKE_OK (three complete games, both starting orders, exit 0), including an explicit no-modal assertion for two-card clicks and exact discard prompt visibility. Updated executable and screenshots copied into the main project.


## Board navigation, screen-edge dock and rounded 3D dice

`BoardNavigation.cs` handles left-drag and wheel events on the map only, with a 100%–200% zoom range, ten percentage points per wheel notch and pointer-centered scaling. Actual projected board bounds are constrained so the overlap with the screen is at least 60% of each dimension. On window resize, the baseline fit is recalculated to ensure that constraint remains possible at 200%. UI surfaces consume pointer input before it can reach the board.

`ScreenEdgeDock.cs` pins the sidebar group to equal right/top/bottom screen margins, reflows the bottom hand position, and keeps draw/discard animation destinations aligned with the hand and deck.

`TabletopDie.cs` creates a smooth beveled cube mesh, 21 raised/inset-looking pip meshes across all six faces, an independent camera/light and a contact shadow. A bounded rolling/tumbling/bouncing animation settles with the chosen game result facing up. The rules still choose an unbiased random value from 1–6; the visual animation does not change that value. Pause and scene cleanup cover the dice as well as the board.

Regression checks include idle-hand hover suppression, pointer drag extremes, 100%/200% zoom limits, ten-percent wheel increments, per-axis 60% visibility, all six upward dice values and rolling-to-settled rendering. Screenshots include BoardZoom200, DiceRolling and DiceSettled.

Final navigation/dice verification: navigation-build.log records TRAIL_BUILD_OK and rules success. navigation-smoke.log records TRAIL_SMOKE_OK, three complete games with both starting orders, exit 0; the same run checked idle hover suppression, wheel normalization, drag/zoom limits, per-axis visibility and dice orientation/settling. The installed Windows executable and docs/previews screenshots are updated.
