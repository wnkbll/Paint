﻿namespace Paint.Interfaces;

internal interface ICanvasSizeDepends {
    public Size CanvasSize { get; set; }
}

internal interface IState {
    public void MouseUpHandler(object sender, MouseEventArgs e);
    public void MouseMoveHandler(object sender, MouseEventArgs e);
    public void MouseDownHandler(object sender, MouseEventArgs e);
}