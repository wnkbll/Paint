﻿using Paint.depricated.Interfaces;
using Paint.Serialization;
using Paint.Serialization.Models;
using Paint.States;
using Paint.States.depricated;
using Paint.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Paint;

internal partial class MainForm : Form {
    private Size CanvasSize { get; set; }
    private int PenSize { get; set; } = 2;
    private Color PenColor { get; set; } = Color.Black;
    private bool IsFilling { get; set; } = false;
    private Color BrushColor { get; set; } = Color.White;
    private Font TextFont { get; set; } = new("Times New Roman", 12.0f);
    private Dictionary<FigureTypes, Tuple<ToolStripButton, ToolStripMenuItem>> FigureButtons { get; set; }
    private Dictionary<StatesEnum, Tuple<ToolStripButton, ToolStripMenuItem>> StateButtons { get; set; }
    private bool SnapToGrid { get; set; } = false;

    private class ToolStripRenderer : ToolStripProfessionalRenderer {
        public ToolStripRenderer() : base() {
            this.RoundedEdges = false;
        }
    }

    public MainForm() {
        this.InitializeComponent();

        this.ToolStrip.Renderer = new ToolStripRenderer();

        this.FigureButtons = new() {
            {FigureTypes.Rectangle, new(this.RectangleButton, this.RectangleToolButton)},
            {FigureTypes.Ellipse, new(this.EllipseButton, this.EllipseToolButton)},
            {FigureTypes.StraightLine, new(this.StraightLineButton, this.StraightLineToolButton)},
            {FigureTypes.CurveLine, new(this.CurveLineButton, this.CurveLineToolButton)},
            {FigureTypes.TextBox, new(this.TextButton, this.TextToolButton)},
        };

        this.StateButtons = new() {
            {StatesEnum.DrawState, new(this.DrawingButton, this.DrawingToolButton)},
            {StatesEnum.SelectState, new(this.SelectionButton, this.SelectionToolButton)},
            {StatesEnum.EditState, new(this.EditButton, this.EditToolButton)},
        };
    }

    public void UpdatePointerInfo(Point point) {
        this.PointerInfo.Text = $"{point.X}, {point.Y} px";
    }

    public void UpdateCanvasInfo(Size canvasSize) {
        this.CanvasSize = new Size(canvasSize.Width, canvasSize.Height);
        this.CanvasInfo.Text = $"{canvasSize.Width} x {canvasSize.Height}";
    }

    public void UpdatePenInfo(Color color, int size) {
        this.PenColor = color;
        this.PenSize = size;

        string hexColor = Convert.ToHexString(
            [color.A, color.R, color.G, color.B]
        );

        if (color.IsNamedColor) {
            this.PenInfo.Text = $"{color.Name} ({hexColor}), {size} px";
        }

        this.PenInfo.Text = $"{hexColor}, {size} px";

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.PenColor = this.PenColor;
            state.PenSize = this.PenSize;
        }
    }

    private void UpdateBrushInfo(Color color) {
        this.BrushColor = color;

        string hexColor = Convert.ToHexString(
            [color.A, color.R, color.G, color.B]
        );

        if (color.IsNamedColor) {
            this.BrushInfo.Text = $"{color.Name} ({hexColor})";
            return;
        }

        this.BrushInfo.Text = $"{hexColor}";

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.BrushColor = this.BrushColor;
        }
    }

    private void UpdateFontInfo(Font font) {
        this.TextFont = font;

        this.FontInfo.Text = $"{font.Name}, {font.SizeInPoints} pt";

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.TextFont = this.TextFont;
        }
    }

    private void CheckFigureButton(FigureTypes? figureType) {
        if (figureType is null) {
            foreach (KeyValuePair<FigureTypes, Tuple<ToolStripButton, ToolStripMenuItem>> buttons in this.FigureButtons) {
                buttons.Value.Item1.Checked = false;
                buttons.Value.Item2.Checked = false;
            }
        }

        foreach (KeyValuePair<FigureTypes, Tuple<ToolStripButton, ToolStripMenuItem>> buttons in this.FigureButtons) {
            if (buttons.Key == figureType) {
                buttons.Value.Item1.Checked = true;
                buttons.Value.Item2.Checked = true;
                continue;
            }

            buttons.Value.Item1.Checked = false;
            buttons.Value.Item2.Checked = false;
        }
    }

    private void CheskStateButton(StatesEnum stateType) {
        if (stateType != StatesEnum.DrawState) {
            foreach (KeyValuePair<FigureTypes, Tuple<ToolStripButton, ToolStripMenuItem>> buttons in this.FigureButtons) {
                buttons.Value.Item1.Enabled = false;
                buttons.Value.Item2.Enabled = false;
            }
        } else {
            foreach (KeyValuePair<FigureTypes, Tuple<ToolStripButton, ToolStripMenuItem>> buttons in this.FigureButtons) {
                buttons.Value.Item1.Enabled = true;
                buttons.Value.Item2.Enabled = true;
            }
        }

        foreach (KeyValuePair<StatesEnum, Tuple<ToolStripButton, ToolStripMenuItem>> buttons in this.StateButtons) {
            if (buttons.Key == stateType) {
                buttons.Value.Item1.Checked = true;
                buttons.Value.Item2.Checked = true;
                continue;
            }

            buttons.Value.Item1.Checked = false;
            buttons.Value.Item2.Checked = false;
        }
    }

    private void OnLoad(object sender, EventArgs e) {
        this.UpdateFontInfo(this.TextFont);
        this.UpdatePenInfo(this.PenColor, this.PenSize);
        this.UpdateBrushInfo(this.BrushColor);
    }

    private void NewCanvasButtonClick(object sender, EventArgs e) {
        var canvasSizeWindow = new CanvasSizeForm(this);

        if (canvasSizeWindow.ShowDialog() == DialogResult.OK) {
            var state = new DrawState() {
                CanvasSize = this.CanvasSize,
                PenColor = this.PenColor,
                PenSize = this.PenSize,
                BrushColor = this.BrushColor,
                TextFont = this.TextFont,
                FigureType = FigureTypes.Rectangle,
            };

            if (this.CanvasSize.Width > 0 && this.CanvasSize.Height > 0) {
                var canvasWindow = new CanvasForm() {
                    MdiParent = this,
                    Text = "Рисунок " + this.MdiChildren.Length.ToString(),
                    State = state,
                    Size = this.CanvasSize
                };
                state.ParentReference = canvasWindow;
                canvasWindow.State = state;

                canvasWindow.Show();
            }
        }
    }

    private void SaveCanvasButtonClick(object sender, EventArgs e) {
        if (this.ActiveMdiChild is CanvasForm activeForm) {
            JsonReader.Save(activeForm.Size, activeForm.Figures);
        }
    }

    private void OpenCanvasButtonClick(object sender, EventArgs e) {
        HashableCanvas? canvas = JsonReader.Open();

        if (canvas is null) {
            return;
        }

        List<IDrawable> figures = JsonReader.ToDrawable(canvas.Figures);

        this.CanvasSize = new Size(canvas.CanvasSize.Item1, canvas.CanvasSize.Item2);
        this.UpdateCanvasInfo(new Size(canvas.CanvasSize.Item1, canvas.CanvasSize.Item2));

        var state = new DrawState() {
            PenColor = this.PenColor,
            PenSize = this.PenSize,
            BrushColor = this.BrushColor,
            TextFont = this.TextFont,
            FigureType = FigureTypes.Rectangle,
            Figures = figures,
            CanvasSize = this.CanvasSize,
        };

        if (this.CanvasSize.Width > 0 && this.CanvasSize.Height > 0) {
            var canvasWindow = new CanvasForm() {
                MdiParent = this,
                Text = "Рисунок " + this.MdiChildren.Length.ToString(),
                Size = this.CanvasSize,
                State = state,
                Figures = figures,
            };
            state.ParentReference = canvasWindow;
            canvasWindow.State = state;
            canvasWindow.Show();
        }
    }

    private void PenSizeButtonClick(object sender, EventArgs e) {
        var penSizeWindow = new PenSizeForm(this, this.PenColor);

        if (penSizeWindow.ShowDialog() == DialogResult.OK) {
            return;
        }
    }

    private void PenColorButtonClick(object sender, EventArgs e) {
        var colorDialog = new ColorDialog();
        _ = colorDialog.ShowDialog();

        this.UpdatePenInfo(colorDialog.Color, this.PenSize);
    }

    private void BrushColorButtonClick(object sender, EventArgs e) {
        var colorDialog = new ColorDialog();
        _ = colorDialog.ShowDialog();

        this.UpdateBrushInfo(colorDialog.Color);
    }

    private void FillingButtonClick(object sender, EventArgs e) {
        if (this.IsFilling) {
            this.IsFilling = false;
            this.FillingButton.Checked = false;
            this.FillingToolButton.Checked = false;

            return;
        }

        this.IsFilling = true;
        this.FillingButton.Checked = true;
        this.FillingToolButton.Checked = true;

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.IsFilling = this.IsFilling;
        }
    }

    private void FontButtonClick(object sender, EventArgs e) {
        _ = this.FontDialog.ShowDialog();
        this.FontDialog.Font = this.TextFont;

        this.UpdateFontInfo(this.FontDialog.Font);
    }

    private void RectangleButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.Rectangle);

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.FigureType = FigureTypes.Rectangle;
        }
    }

    private void EllipseButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.Ellipse);

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.FigureType = FigureTypes.Ellipse;
        }
    }

    private void StraightLineButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.StraightLine);

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.FigureType = FigureTypes.StraightLine;
        }
    }

    private void CurveLineButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.CurveLine);

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.FigureType = FigureTypes.CurveLine;
        }
    }

    private void TextButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.TextBox);

        if (this.ActiveMdiChild is CanvasForm children && children.State is DrawState state) {
            state.FigureType = FigureTypes.TextBox;
        }
    }

    private void DrawingButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(FigureTypes.Rectangle);
        this.CheskStateButton(StatesEnum.DrawState);

        if (this.ActiveMdiChild is CanvasForm child) {

            if (!child.ShowGrid) {
                this.SnapToGrid = false;
                this.SnapToGridToolButton.Checked = false;
            }

            var state = new DrawState() {
                PenColor = this.PenColor,
                PenSize = this.PenSize,
                BrushColor = this.BrushColor,
                TextFont = this.TextFont,
                FigureType = FigureTypes.Rectangle,
                Figures = child.Figures,
                CanvasSize = child.Size,
                ParentReference = child,
                SnapToGrid = this.SnapToGrid,
            };
            child.SelectedFigures.Clear();
            child.State = state;
        }
    }

    private void SelectionButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(null);
        this.CheskStateButton(StatesEnum.SelectState);

        if (this.ActiveMdiChild is CanvasForm child) {

            var state = new SelectState() {
                Figures = child.Figures,
                CanvasSize = child.Size,
            };
            state.SelectedFigures.Clear();
            child.SelectedFigures.Clear();
            child.State = state;
        }
    }

    private void EditButtonClick(object sender, EventArgs e) {
        this.CheckFigureButton(null);
        this.CheskStateButton(StatesEnum.EditState);

        if (this.ActiveMdiChild is CanvasForm child) {
            var state = new EditState() {
                Figures = child.Figures,
                CanvasSize = child.Size,
            };
            state.SelectedFigures.Clear();
            child.SelectedFigures.Clear();
            child.State = state;

            foreach (Form mdiChild in this.MdiChildren) {
                if (mdiChild is EditForm) {
                    return;
                }
            }

            var editTable = new EditForm() {
                MdiParent = this,
            };

            editTable.Show();
        }
    }

    private void GridToolButtonClick(object sender, EventArgs e) {
        if (this.ActiveMdiChild is CanvasForm activeCanvas) {

            activeCanvas.ToggleGrid();

            if (!activeCanvas.ShowGrid) {
                if (activeCanvas.State is DrawState drawState) {
                    drawState.SnapToGrid = false;
                    this.SnapToGridToolButton.Checked = false;
                }
            }

            if (activeCanvas.ShowGrid && this.SnapToGrid) {
                if (activeCanvas.State is DrawState drawState) {
                    drawState.SnapToGrid = true;
                    this.SnapToGridToolButton.Checked = true;
                }
            }

            this.GridToolButton.Checked = activeCanvas.ShowGrid;
            this.SnapToGridToolButton.Enabled = activeCanvas.ShowGrid;
        }
    }

    private void SnapToGridToolButtonClick(object sender, EventArgs e) {
        if (this.ActiveMdiChild is CanvasForm canvasWindow) {

            if (!canvasWindow.ShowGrid) {
                this.SnapToGrid = false;
                this.SnapToGridToolButton.Checked = false;
                return;
            }

            this.SnapToGrid = !this.SnapToGrid;
            this.SnapToGridToolButton.Checked = this.SnapToGrid;

            if (canvasWindow.State is DrawState drawState) {
                drawState.SnapToGrid = this.SnapToGrid;
            }
        }
    }

    private void UpdateGridButtonState() {
        if (this.ActiveMdiChild is CanvasForm canvasWindow) {

            int gridStep = canvasWindow.GetGridStep();

            this.DefaultGridStepToolButton.Checked = gridStep == 10;
            this.MaxGridStepToolButton.Checked = gridStep == 50;
        }
    }

    private void DefaultGridStepToolButtonClick(object sender, EventArgs e) {
        if (this.ActiveMdiChild is CanvasForm canvasWindow) {
            canvasWindow.SetGridStep(10);
            this.UpdateGridButtonState();
        }
    }

    private void MaxGridStepToolButtonClick(object sender, EventArgs e) {
        if (this.ActiveMdiChild is CanvasForm canvasWindow) {
            canvasWindow.SetGridStep(50);
            this.UpdateGridButtonState();
        }
    }
}