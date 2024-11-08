import {makeScene2D, Rect, lines, CubicBezier, Layout} from '@motion-canvas/2d';
import {all, createRef, DEFAULT, easeInCubic, easeOutCubic, spawn, waitFor} from '@motion-canvas/core';
import { CodeWindow } from '../components/code-window';

export default makeScene2D(function* (view) {

  // fill background
  // display three functions in center, in padded flexbox:
  //    CalculateCatchChance()
  //    ChooseLootTable()
  //    RollItems()
  //

  const fishingProcedureCodeString = `\
func FishingProcedure():

  CheckIfAnythingCaught()

  ChooseLootTable()

  PickCaughtItem()`

  let fishingProcedureContainer = createRef<Layout>()
  let fishingProcedureWindow = createRef<CodeWindow>()

  let container = createRef<Rect>()

  view.add(
    //<Rect size={"100%"} fill={"#242424"} ref={container}>
    <Rect size={"100%"} fill={"#242424"} ref={container}>
      <Layout layout ref={fishingProcedureContainer}>
        <CodeWindow title="Fishing Procedure" code={fishingProcedureCodeString} scale={0} ref={fishingProcedureWindow}/>
      </Layout>
    </Rect>
  )

  yield* waitFor(1)
  yield* fishingProcedureWindow().scale(1, 0.25)
  yield* waitFor(1)

  yield* highlightSequence(
    fishingProcedureWindow(),
    [2, 4, 6, 2],
    0.6
  )

  // CATCH CHANCE FUNCTION
  const catchChanceCode = `\
func CheckIfAnythingCaught():

  BaseChance = CurrentBait.BaseChance
  FailedCastChance = BaseChance * FailedCastMultiplier
  RodCatchChance = BaseChance * RodCatchLevel * 0.02
  ZoneChance = BaseChance * CurrentZone.CatchBoostMultiplier
  FishChance = BaseChance + FailedCastChance + RodCatchChance + ZoneChance

  if RecentReel > 0:
      FishChance *= 1.1

  if Lure is "Attractive":
      FishChance *= 1.3

  if Player in Rain:
      FishChance *= 1.1

  FishChance *= DrinkMultiplier

  if Random() > FishChance:
    # you didnt catch anything...
    FailedCastMultiplier += 0.05
    return

  # you caught something!!!
  FailedCastMultiplier = 0`

  let catchChanceWindow = createRef<CodeWindow>()

  view.add(
    <Layout layout direction={"row-reverse"} position={container().right().addX(-100)} offset={[1, 0]}>
      <CodeWindow
        title="Catching Something"
        code={catchChanceCode}
        codeFontSize={24}
        scale={0}
        ref={catchChanceWindow}/>
    </Layout>
  )

  yield* all(
    //fishingProcedureContainer().offset([-1, 0], 0.2),
    fishingProcedureWindow().position([-container().size().x * 0.3, 0], 0.2),
    catchChanceWindow().scale(1, 0.2),
  )

  yield* waitFor(1)

  yield* highlightSequence(
    catchChanceWindow(),
    [2, 3, 4, 5, 6, [8, 9], [11, 13], [14, 15], 17, [19, 22], [24, 25], DEFAULT],
    0.4
  )
  // CATCH CHANCE FUNCTION END

  // CHOOSE LOOT TABLE FUNCTION
  const chooseTableCode = `\
func ChooseLootTable():

  LootTable = Saltwater

  if Zone is Freshwater:
      LootTable = Freshwater

  if Zone is Meteor:
      LootTable = Alien

  # small chance that the player fishes water trash when not fishing a meteor
  # and not fishing in fish spawn
  WaterTrashChance = 0.05
  if Lure is "Magnet":
    WaterTrashChance = 0.1

  if (Zone not in [FishSpawn, Meteor]) and (Random() < WaterTrashChance):
      LootTable = WaterTrash

  # small chance that the player fishes a rain fish in the rain
  if PlayerInRain and Random() < 0.08:
      LootTable = Rain`

  let chooseTableWindow = createRef<CodeWindow>()

  view.add(
    <Layout layout direction={"row-reverse"} position={catchChanceWindow().parentAs<Layout>().middle().addX(150)}>
      <CodeWindow
        title="Choosing a Loot Table"
        code={chooseTableCode}
        codeFontSize={24}
        opacity={0}
        ref={chooseTableWindow}/>
    </Layout>
  )

  yield* all(
    catchChanceWindow().position(catchChanceWindow().position().addX(-150), 0.75, easeOutCubic),
    catchChanceWindow().opacity(0, 0.75, easeOutCubic),
    chooseTableWindow().position(chooseTableWindow().position().addX(-150), 0.75, easeOutCubic),
    chooseTableWindow().opacity(1, 0.75, easeOutCubic),
    fishingProcedureWindow().codeNode().selection(lines(4), 0.6),
  )
  catchChanceWindow().parent().remove()

  yield* highlightSequence(
    chooseTableWindow(),
    [2, [4, 5], [7, 8], [9, 14], [16, 18], DEFAULT],
    0.4
  )
  // CHOOSE LOOT TABLE END

  // TODO: explain item properties

  // PICK ITEMS FUNCTION
  const pickItemCode = `\
func PickCaughtItem():

  Items = LootTable.RollThreeItemsAndSizes()

  match Lure:
    is Small:
      ChosenItem = Items.Smallest()
    is Large:
      ChosenItem = Items.Largest()
    is Sparkling:
      ChosenItem = Items.LastHighestTier()
    is Gold:
      ChosenItem = Items.LastRare()
    otherwise:
      ChosenItem = Items.Last()

  if Random() < 0.02 * TreasureMultiplier:
    ChosenItem = TreasureChest`

  let pickItemWindow = createRef<CodeWindow>()

  view.add(
    <Layout layout direction={"row-reverse"} position={chooseTableWindow().parentAs<Layout>().middle().addX(150)}>
      <CodeWindow
        title="Picking The Item"
        code={pickItemCode}
        codeFontSize={24}
        opacity={0}
        ref={pickItemWindow}/>
    </Layout>
  )

  yield* all(
    chooseTableWindow().position(chooseTableWindow().position().addX(-150), 0.75, easeOutCubic),
    chooseTableWindow().opacity(0, 0.75, easeOutCubic),
    pickItemWindow().position(chooseTableWindow().position().addX(-150), 0.75, easeOutCubic),
    pickItemWindow().opacity(1, 0.75, easeOutCubic),
    fishingProcedureWindow().codeNode().selection(lines(6), 0.6),
  )
  chooseTableWindow().parent().remove()

  yield* highlightSequence(
    pickItemWindow(),
    [2, [4, 14], [5, 6], [7, 8], [9, 10], [11, 12], [13, 14], [16, 17], DEFAULT],
    0.4
  )
  // PICK ITEMS END
})

function* highlightSequence(codeWindow: CodeWindow, sequence: Array<typeof DEFAULT | number | Array<number>>, time: number) {
  for (const line of sequence) {
    if (line == DEFAULT)
      yield* codeWindow.codeNode().selection(DEFAULT, time)
    else if (line instanceof Array)
      yield* codeWindow.codeNode().selection(lines(line[0], line[1]), time)
    else
      yield* codeWindow.codeNode().selection(lines(line), time)
    yield* waitFor(1)
  }
}