---
paths:
  - HomeAutomationClient/HomeAutomationClient/Assets/Images/**
---

# Icons in Assets/Images

## Zero margin
- Every icon XAML in `Assets/Images` fills its box exactly: the `Canvas` (or the `Path` with `Stretch="None"`) is
  as wide and as high as the drawing itself, stroke included, and the drawing starts at its top left corner. The
  icon brings no margin of its own; whoever places it decides on the spacing with `Margin` there.
- The box need not be square. A 22 by 21 house is a 22 by 21 box, not a 24 by 24 one.
- Stroke counts. A stroked shape reaches half its `StrokeThickness` beyond its geometry (round joins and caps
  included), so the box is the geometry plus that on every side, and the `Canvas.Left` / `Canvas.Top` (or a
  `TranslateTransform`) moves the geometry so that this edge lies at 0. When the stroke width changes, the box and
  the offsets change with it.
- Measure, do not estimate: take the geometry's bounds from a renderer (a browser's `getBBox()` on the same path
  data works), then add the stroke. Write the numbers you used into a comment in the file, so the next change can
  redo the sum.
- An icon with several states that swap in place (`VisibilityIcon`) shares one box for all of them: the union of the
  states' bounds, each state placed in it so that the swap does not jump.
