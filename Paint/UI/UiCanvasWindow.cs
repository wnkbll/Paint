﻿using Paint.Interfaces;
using Paint.Serialization;
using Paint.States;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Paint;

internal partial class UiCanvasWindow : Form {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public required IState State { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<IDrawable> Figures { get; set; } = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<IDrawable> SelectedFigures { get; set; } = [];

    private IDrawable? DashFigure { get; set; } = null;
    private bool IsAbleToUpdate { get; set; } = false;
    private BufferedGraphics? GraphicsBuffer { get; set; }

    private bool ShowGrid { get; set; } = false;
    private int GridStep { get; set; } = 50;
    private Color GridColor { get; set; } = Color.Gray;

    public UiCanvasWindow() {
        this.SetDoubleBuffering(true);
        this.InitializeComponent();
    }

    private void SetDoubleBuffering(bool isEnable) {
        this.SetStyle(
            ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, isEnable
        );

        this.UpdateStyles();
    }

    private void UpdateGraphicsBuffer(Rectangle bufferArea) {
        BufferedGraphicsContext bufferedContext = BufferedGraphicsManager.Current;
        this.GraphicsBuffer = bufferedContext.Allocate(this.CreateGraphics(), bufferArea);

        if (this.State is DrawState state) {
            state.GraphicsBuffer = this.GraphicsBuffer;
        }
    }

    private void DeleteSelectedFigures() {
        foreach (IDrawable figure in this.SelectedFigures) {
            _ = this.Figures.Remove(figure);
        }

        this.SelectedFigures.Clear();
    }

    private void CopySelectedFiguresToClipboard() {
        if (this.SelectedFigures.Count == 0) {
            return;
        }

        string json = JsonReader.ToBufferString(this.SelectedFigures);

        Clipboard.SetText(json);
    }

    private void PasteFiguresFromClipboard() {
        if (!Clipboard.ContainsText()) {
            return;
        }

        string json = Clipboard.GetText();
        try {
            List<IDrawable> figures = JsonReader.ToFigureList(json);

            if (figures.Count == 0) {
                return;
            }

            Point mousePosition = this.PointToClient(Cursor.Position);
            OffsetFiguresToMousePosition(figures, mousePosition);
            this.Figures.AddRange(figures);

        } catch (Exception) {
            return;
        }
    }

    private static void OffsetFiguresToMousePosition(List<IDrawable> figures, Point mousePosition) {
        if (figures.Count == 0) {
            return;
        }

        IDrawable firstFigure = figures[0];
        int offsetX = mousePosition.X - firstFigure.TopPoint.X;
        int offsetY = mousePosition.Y - firstFigure.TopPoint.Y;

        foreach (IDrawable figure in figures) {
            figure.Move(offsetX, offsetY);
        }
    }

    private void DrawFigures(BufferedGraphics graphicsBuffer) {
        Graphics graphics = graphicsBuffer.Graphics;
        this.DrawGrid(graphics);
        if (this.State is DrawState) {
            this.DashFigure?.DrawDash(graphics);

            foreach (IDrawable figure in this.Figures) {
                figure.Draw(graphics);
            }
        }

        if (this.State is SelectState) {
            foreach (IDrawable figure in this.Figures) {
                if (this.SelectedFigures.Contains(figure)) {
                    figure.DrawSelection(graphics);
                } else {
                    figure.Draw(graphics);
                }
            }
        }

        if (this.State is EditState) {

        }


        graphicsBuffer.Render();
    }

    private void OnRender(object? sender, EventArgs e) {
        var background = new Rectangle(0, 0, this.Size.Width, this.Size.Height);

        if (this.GraphicsBuffer is null) {
            this.UpdateGraphicsBuffer(background);
        }

        if (this.GraphicsBuffer is not null) {
            IsAbleToUpdate = false;
            Graphics graphics = this.GraphicsBuffer.Graphics;
            graphics.Clear(Color.White);
            this.DrawFigures(this.GraphicsBuffer);
            IsAbleToUpdate = true;
        }
    }

    private void OnLoad(object sender, EventArgs e) {
        var timer = new System.Timers.Timer() {
            Interval = 0.00001,
        };

        timer.Elapsed += this.OnRender;
        timer.Start();
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private void OnMouseDown(object sender, MouseEventArgs e) {
        if (this.State is DrawState drawing) {
            drawing.MouseDownHandler(e);

            this.Figures = drawing.Figures;
            this.DashFigure = drawing.DashFigure;
        }

        if (this.State is SelectState selection) {
            selection.MouseDownHandler(e);

            this.Figures = selection.Figures;
            this.SelectedFigures = selection.SelectedFigures;
        }

        if (this.State is EditState) {
            return;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
        if (this.MdiParent is UiMainWindow parent) {
            parent.UpdatePointerInfo(new Point(e.X, e.Y));
        }

        if (this.State is DrawState drawing && drawing.IsDrawing) {
            drawing.MouseMoveHandler(e);

            this.Figures = drawing.Figures;
            this.DashFigure = drawing.DashFigure;
        }

        if (this.State is SelectState selection) {
            selection.MouseMoveHandler(e);

            this.SelectedFigures = selection.SelectedFigures;
            this.Figures = selection.Figures;
        }

        if (this.State is EditState) {
            return;
        }
    }

    private void OnMouseUp(object sender, MouseEventArgs e) {
        if (this.State is DrawState drawing) {
            drawing.MouseUpHandler(e);

            this.Figures = drawing.Figures;
            this.DashFigure = drawing.DashFigure;
        }

        if (this.State is SelectState selection) {
            selection.MouseUpHandler(e);

            this.Figures = selection.Figures;
            this.SelectedFigures = selection.SelectedFigures;
        }

        if (this.State is EditState) {
            return;
        }
    }

    private void OnClose(object sender, FormClosingEventArgs e) {
        if (this.Figures.Count > 0) {
            DialogResult response = MessageBox.Show("Вы хотите сохранить изменения в документе?", "Attention", MessageBoxButtons.YesNoCancel);

            if (response == DialogResult.Yes) {
                JsonReader.Save(this.Size, this.Figures);
            } else if (response == DialogResult.Cancel) {
                e.Cancel = true;
            }
        }
    }

    private void OnResize(object sender, EventArgs e) {
        if (this.MdiParent is UiMainWindow parent) {
            parent.UpdateCanvasInfo(this.Size);
        }

        if (this.State is not null) {
            this.State.CanvasSize = this.Size;
        }

        if (IsAbleToUpdate) {
            this.GraphicsBuffer = null;
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {

        if (this.State is SelectState) {
            if (e.KeyData == Keys.Delete) {
                this.DeleteSelectedFigures();
            }

            if (e.Control && e.KeyCode == Keys.C) {
                this.CopySelectedFiguresToClipboard();
            }

            if (e.Control && e.KeyCode == Keys.X) {
                this.CopySelectedFiguresToClipboard();
                foreach (IDrawable figure in this.SelectedFigures) {
                    this.Figures.Remove(figure);
                }
                this.SelectedFigures.Clear();
            }
        }

        if (this.State is DrawState) {
            if (e.Control && e.KeyCode == Keys.V) {
                this.PasteFiguresFromClipboard();
            }
        }
    }

    private void DrawGrid(Graphics g) {
        if (!ShowGrid || GridStep <= 0) return;

        using var pen = new Pen(GridColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };

        for (int x = GridStep ; x < this.Size.Width ; x += GridStep) {
            g.DrawLine(pen, x, 0, x, this.Size.Height);
        }

        for (int y = GridStep ; y < this.Size.Height ; y += GridStep) {
            g.DrawLine(pen, 0, y, this.Size.Width, y);
        }
    }
    public void ToggleGrid() {
        this.ShowGrid = !this.ShowGrid; 
        this.Invalidate();             
    }
}
