using HarmonyLib;
using System;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace ImprovedHandbookRecipes;
public abstract class ButtonRTC : RichTextComponentBase {
    private const double UnscaledSize = 24.0;
    private const double Margin       =  2.0;

    private readonly int index;
    private readonly string label;
    private readonly string tooltip;
    private readonly double offsetX;
    private readonly double offsetY;
    private double timeInside = 0.0;

    private readonly GuiElementTextButton button;
    private readonly GuiElementHoverText hover;
    private readonly ElementBounds bounds;

    public ButtonRTC(ICoreClientAPI api, int index, string label, string key, double offsetX, double offsetY) : base(api) {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
        this.index = index;
        this.label = label;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        tooltip = Lang.Get("improvedhandbookrecipes:" + key);

        double size = Math.Ceiling(GuiElement.scaled(UnscaledSize));
        bounds = new GlobalBounds(0.0, 0.0, size, size);
        button = CreateButton(bounds);
        hover = CreateHover(bounds);
    }

    private GuiElementTextButton CreateButton(ElementBounds bounds) {
        var font     = CairoFont.ButtonText();
        var fontDown = CairoFont.ButtonPressedText();
        font.UnscaledFontsize = fontDown.UnscaledFontsize = GuiElement.scaled(32.0);
        var button = new GuiElementTextButton(api, label, font, fontDown, Click, bounds, EnumButtonStyle.Small);
        button.PlaySound = false;

        var traverse = Traverse.Create(button);
        AdjustOffsets(traverse.Field<GuiElementStaticText>("normalText").Value);
        AdjustOffsets(traverse.Field<GuiElementStaticText>("pressedText").Value);
        button.ComposeElements(null, null);

        return button;
    }

    private GuiElementHoverText CreateHover(ElementBounds bounds) {
        var hover = new GuiElementHoverText(api, tooltip, CairoFont.WhiteSmallText(), 200, bounds);
        hover.SetAutoDisplay(false);
        return hover;
    }

    protected void SetExtraTip(string text = null) 
        => hover.SetNewText((text == null) ? tooltip : $"{tooltip}\n{text}");

    protected abstract void OnClick();

    private bool Click() {
        OnClick();
        return true;
    }

    protected virtual bool Visible 
        => true;

    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX) {
        double x = offsetX - GuiElement.scaled(3.0);
        double y = lineY + GuiElement.scaled(126.0 - UnscaledSize - index * (UnscaledSize + Margin));
        double size = GuiElement.scaled(UnscaledSize);
        BoundsPerLine = new LineRectangled[] { new(x, y, size, size) };

        bounds.fixedWidth = bounds.fixedHeight = size;
 
        nextOffsetX = offsetX;
        return EnumCalcBoundsResult.Continue;
    }

    private void AdjustOffsets(GuiElementStaticText elem) {
        elem.offsetX = GuiElement.scaled(GuiElement.scaled(offsetX));
        elem.offsetY = GuiElement.scaled(GuiElement.scaled(offsetY));
    }

    public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ) {
        if (Visible) {
            SetBounds(renderX, renderY);
            button.RenderInteractiveElements(deltaTime);
            hover.SetVisible(MouseOverFor(1.0, deltaTime));
            hover.RenderInteractiveElements(deltaTime);
        }
    }

    private bool MouseOverFor(double time, double delta) {
        if (bounds.PointInside(api.Input.MouseX, api.Input.MouseY)) {
            timeInside += delta;
        } else {
            timeInside = 0.0;
        }
        return timeInside > time;
    } 

    private void SetBounds(double xOffset = 0.0, double yOffset = 0.0) {
        var r = BoundsPerLine[0];
        bounds.absInnerWidth  = r.Width;
        bounds.absInnerHeight = r.Height;
        bounds.absFixedX = xOffset + r.X;
        bounds.absFixedY = yOffset + r.Y;
    }

    public override void OnMouseDown(MouseEvent args) {
        if (Visible) {
            SetBounds();
            button.OnMouseDown(api, args);
        }
    }

    public override void OnMouseUp(MouseEvent args) {
        if (Visible) {
            SetBounds();
            button.OnMouseUp(api, args);
        }
    }

    public override void OnMouseMove(MouseEvent args) {
        if (Visible) {
            button.PlaySound = true;
            button.OnMouseMove(api, args);
            button.PlaySound = false;
        }
    }

    public override void Dispose() {
        button.Dispose();
        hover.Dispose();
    }

    private class GlobalBounds : ElementBounds {
        public GlobalBounds(double x, double y, double width, double height) { 
            absFixedX = x;
            absFixedX = y;
            absInnerWidth  = fixedWidth  = width;
            absInnerHeight = fixedHeight = height;
            BothSizing = ElementSizing.Fixed;
            ParentBounds = new();
        }

        public ElementBounds MakeChild() 
            => Fill.WithParent(this);

        public override double bgDrawX => absFixedX;

        public override double bgDrawY => absFixedY;

        public override double renderX => absFixedX + renderOffsetX;

        public override double renderY => absFixedY + renderOffsetY;

        public override double absX => absFixedX;

        public override double absY => absFixedY;
    }
}
