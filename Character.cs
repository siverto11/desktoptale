﻿using System;
using Desktoptale.Messages;
using Desktoptale.States.Common;
using Messaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Desktoptale;

public abstract class Character : IGameObject
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Scale;
    
    public InputManager InputManager { get; }
    public StateMachine<Character> StateMachine { get; protected set; }
    public IState<Character> IdleState { get; protected set; }
    public IState<Character> WalkState { get; protected set; }
    public IState<Character> RunState { get; protected set; }
    
    public IAnimatedSprite IdleSprite { get; protected set; }
    public IAnimatedSprite WalkSprite { get; protected set; }
    public IAnimatedSprite RunSprite { get; protected set; }
    public IAnimatedSprite CurrentSprite { get; set; }

    protected virtual IState<Character> InitialState => IdleState;
    
    private GameWindow window;
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private bool dragging;
    private Point previousSpriteFrameSize;
    
    private Subscription scaleChangeRequestedSubscription;

    public Character(GraphicsDeviceManager graphics, GameWindow window, SpriteBatch spriteBatch, InputManager inputManager)
    {
        this.spriteBatch = spriteBatch;
        this.InputManager = inputManager;
        this.graphics = graphics;
        this.window = window;
    }

    public void UpdateSprite(IAnimatedSprite sprite)
    {
        CurrentSprite?.Stop();
        CurrentSprite = sprite;
        
        if(previousSpriteFrameSize != sprite.FrameSize)
            UpdateWindowSize();

        previousSpriteFrameSize = sprite.FrameSize;
    }

    public virtual void Initialize()
    {
        scaleChangeRequestedSubscription = MessageBus.Subscribe<ScaleChangeRequestedMessage>(OnScaleChangeRequestedMessage);

        IdleState = new IdleState();
        WalkState = new WalkState(90f);
        RunState = new RunState(180f);
        
        StateMachine = new StateMachine<Character>(this, InitialState);
        StateMachine.StateChanged += (state, newState) => UpdateOrientation();
    }

    public virtual void LoadContent(ContentManager contentManager) { }
    
    public virtual void Update(GameTime gameTime)
    {
        StateMachine.Update(gameTime);
        
        UpdateOrientation();
        
        Position.X += (int)Math.Round(Velocity.X);
        Position.Y += (int)Math.Round(Velocity.Y);
        
        DragCharacter();
        
        PreventLeavingScreenArea();

        window.Position = new Point((int)Position.X, (int)Position.Y);
        
        CurrentSprite.Update(gameTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Vector2 center = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2f, graphics.GraphicsDevice.Viewport.Height / 2f);
        Vector2 origin = new Vector2(CurrentSprite.FrameSize.X / 2f, CurrentSprite.FrameSize.Y / 2f);
        CurrentSprite.Draw(spriteBatch, center, Color.White, 0, origin, Scale, SpriteEffects.None, 0);
        spriteBatch.End();
    }

    public virtual void Dispose()
    {
        MessageBus.Unsubscribe(scaleChangeRequestedSubscription);
    }

    private void OnScaleChangeRequestedMessage(ScaleChangeRequestedMessage message)
    {
        Scale = new Vector2(message.ScaleFactor, message.ScaleFactor);
        UpdateWindowSize();
    }

    private void UpdateOrientation()
    {
        Orientation? updatedOrientation = GetOrientationFromVelocity(Velocity);
        if (updatedOrientation != null && CurrentSprite is OrientedAnimatedSprite animatedSprite)
            animatedSprite.Orientation = updatedOrientation.Value;
    }

    private Orientation? GetOrientationFromVelocity(Vector2 input)
    {
        if (input.Y < -float.Epsilon) return Orientation.Up;
        if (input.Y > float.Epsilon) return Orientation.Down;
        if (input.X < -float.Epsilon) return Orientation.Left;
        if (input.X > float.Epsilon) return Orientation.Right;

        return null;
    }

    private void DragCharacter()
    {
        if (InputManager.LeftClickJustPressed &&
            graphics.GraphicsDevice.Viewport.Bounds.Contains(InputManager.PointerPosition))
        {
            dragging = true;
        } else if (!InputManager.LeftClickPressed)
        {
            dragging = false;
        }

        if (dragging)
        {
            Point windowCenter = window.Position -
                                 new Point(graphics.PreferredBackBufferWidth / 2, graphics.PreferredBackBufferHeight / 2);
            Position = (windowCenter + InputManager.PointerPosition).ToVector2();
        }
    }

    private void PreventLeavingScreenArea()
    {
        Point screenSize = GetScreenSize();
        Point windowSize = GetWindowSize();

        if (Position.X < -windowSize.X) Position.X = -windowSize.X;
        if (Position.Y < -windowSize.Y) Position.Y = -windowSize.Y;
        if (Position.X > screenSize.X) Position.X = screenSize.X;
        if (Position.Y > screenSize.Y) Position.Y = screenSize.Y;
    }

    private Point GetScreenSize()
    {
        return new Point(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
            GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
    }

    private Point GetWindowSize()
    {
        return new Point((int)(CurrentSprite.FrameSize.X * Scale.X), (int)(CurrentSprite.FrameSize.Y * Scale.Y));
    }

    private void UpdateWindowSize()
    {
        Point windowSize = GetWindowSize();
        graphics.PreferredBackBufferWidth = windowSize.X;
        graphics.PreferredBackBufferHeight = windowSize.Y;
        graphics.ApplyChanges();
    }
}