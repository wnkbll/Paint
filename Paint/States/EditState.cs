﻿using Paint.Interfaces;

namespace Paint.States;

internal class EditState : IState {
    public List<IDrawable> Figures { get; set; } = [];
    public Size CanvasSize { get; set; }

    public void MouseDownHandler(object sender, MouseEventArgs e) {
        throw new NotImplementedException();
    }

    public void MouseMoveHandler(object sender, MouseEventArgs e) {
        throw new NotImplementedException();
    }

    public void MouseUpHandler(object sender, MouseEventArgs e) {
        throw new NotImplementedException();
    }
}
