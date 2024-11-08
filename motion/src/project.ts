import {makeProject} from '@motion-canvas/core'

import example from './scenes/example?scene'
import circle from './scenes/circle?scene'
import { Code, LezerHighlighter } from '@motion-canvas/2d'
import { parser } from '@gdquest/lezer-gdscript'

import "./global.css"

Code.defaultHighlighter = new LezerHighlighter(parser)

export default makeProject({
  scenes: [example],
})
