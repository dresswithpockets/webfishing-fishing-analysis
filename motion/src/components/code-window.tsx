import { Code, initial, Node, NodeProps, Rect, signal, Txt } from "@motion-canvas/2d"
import { createRef, Reference, SignalValue, SimpleSignal } from "@motion-canvas/core"

export interface CodeWindowProps extends NodeProps {
    code?: SignalValue<string>
    codeFontSize?: SignalValue<number>
    title?: SignalValue<string>
}

export class CodeWindow extends Node {
    @initial("")
    @signal()
    public declare readonly code: SimpleSignal<string, this>

    @initial(24)
    @signal()
    public declare readonly codeFontSize: SimpleSignal<number, this>

    @initial("Title")
    @signal()
    public declare readonly title: SimpleSignal<string, this>

    public declare readonly codeNode: Reference<Code>

    public constructor(props?: CodeWindowProps) {
        super({
            ...props,
        })

        this.codeNode = createRef<Code>()

        let rectNode = <Rect layout direction={"column"}>
            <Rect layout fill={"#1a1a1a"} radius={10} padding={32}>
                <Txt fontFamily={"Comic Sans MS"} fontSize={32} fill={"white"} text={this.title}/>
            </Rect>
            <Rect layout direction={"column"} radius={10} marginTop={16} padding={16} fill={"#1a1a1a"}>
                <Code fontFamily={"Comic Code"} fontSize={this.codeFontSize} code={this.code} ref={this.codeNode}></Code>
            </Rect>
        </Rect>

        this.add(rectNode)
    }
}
