namespace Phy6Sim
{
    // This class contains all the logic for drawing the fish tank scene.
    // Assign an instance of this class to the 'Drawable' property of a GraphicsView in your MAUI page.
    public class FishTankRenderer : IDrawable
    {
        // --- Global Simulation Settings ---
        private const int NUM_FISH = 8;
        private const float FISH_BODY_SHADOW_BLUR = 10f;
        private const float MIN_FISH_SIZE = 5f;
        private const float MAX_FISH_SIZE = 13f;
        private const float PECTORAL_FIN_SCALE = 1.0f;

        private readonly List<Fish> _seaCreatures = new();
        private readonly Clock _clock;
        private readonly Random _random = new();
        private float _width;
        private float _height;

        // A palette of colors to define the different schools of fish
        private static readonly Color[] SchoolColors =
        {
            Color.FromHsla(180 / 360f, 1, 0.7f), // Neon Cyan
            Color.FromHsla(300 / 360f, 1, 0.7f), // Neon Magenta
            Color.FromHsla(60 / 360f, 1, 0.7f),  // Neon Yellow
            Color.FromHsla(240 / 360f, 1, 0.75f),// Neon Blue
            Color.FromHsla(0 / 360f, 1, 0.7f)    // Neon Red
        };

        public FishTankRenderer()
        {
            for (int i = 0; i < NUM_FISH; i++)
            {
                _seaCreatures.Add(new Fish(_random));
            }
            _clock = new Clock(_random);
        }

        // The Draw method is called by the GraphicsView whenever the view needs to be redrawn.
        // To create animation, you will call Invalidate() on your GraphicsView from a timer,
        // which will then trigger this Draw method again.
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            _width = dirtyRect.Width;
            _height = dirtyRect.Height;
            
            // Clear the canvas with black background
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(dirtyRect);

            // Update and draw all the creatures in the tank
            foreach (var creature in _seaCreatures)
            {
                creature.Draw(canvas);
            }

            // Update and draw the clock
            _clock.Draw(canvas, _width, _height);
        }

        // This method should be called from a timer (e.g., IDispatcherTimer) on your MAUI page.
        // For example:
        //
        // IDispatcherTimer timer = Application.Current.Dispatcher.CreateTimer();
        // timer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
        // timer.Tick += (s, e) => 
        // {
        //     _fishTankRenderer.Update();
        //     fishCanvas.Invalidate(); // Where fishCanvas is your GraphicsView
        // };
        // timer.Start();
        public void Update()
        {
            foreach (var creature in _seaCreatures)
            {
                creature.Update(_seaCreatures, _clock, _width, _height);
            }
            _clock.Update(_width, _height);
        }

        // --- Helper Function ---
        private static float LerpAngle(float start, float end, float amount)
        {
            float difference = end - start;
            while (difference < -Math.PI) difference += (float)(Math.PI * 2);
            while (difference > Math.PI) difference -= (float)(Math.PI * 2);
            return start + difference * amount;
        }

        // --- Fish Class ---
        private class Fish
        {
            private float _x, _y, _size, _angle, _speed, _currentSpeed, _targetAngle, _tailAnimation;
            private readonly Color _bodyColor, _tailColor;
            private string _state;
            private double _stateTimer;
            private readonly Random _random;

            public float X => _x;
            public float Y => _y;
            public float Size => _size;
            public Color BodyColor => _bodyColor;
            public float Angle => _angle;

            public Fish(Random random)
            {
                _random = random;
                const float sizeRange = MAX_FISH_SIZE - MIN_FISH_SIZE;
                _size = (float)_random.NextDouble() * sizeRange + MIN_FISH_SIZE;

                int schoolIndex = _random.Next(SchoolColors.Length);
                _bodyColor = SchoolColors[schoolIndex];
                _tailColor = Color.FromHsla((float)_random.NextDouble(), 1, 0.7f);

                _angle = (float)(_random.NextDouble() * Math.PI * 2);
                _speed = (float)_random.NextDouble() * 1.0f + 0.5f;
                _currentSpeed = _speed;

                _state = "swimming";
                _stateTimer = GetRandomTimer();
                _targetAngle = _angle;
                _tailAnimation = (float)_random.NextDouble() * 10;
            }
            
            public void Spawn(float width, float height)
            {
                 _x = (float)_random.NextDouble() * width;
                 _y = (float)_random.NextDouble() * height;
            }

            private double GetRandomTimer() => _random.NextDouble() * 240 + 60;

            public void Draw(ICanvas canvas)
            {
                canvas.SaveState();
                canvas.Translate(_x, _y);
                canvas.Rotate((float)(_angle * 180 / Math.PI));

                float tailWave = (float)Math.Sin(_tailAnimation) * _size * 0.5f;
                float finFlap = (float)Math.Sin(_tailAnimation + 1) * _size * 0.2f;

                // --- Draw Tail ---
                canvas.StrokeSize = 0;
                canvas.FillColor = _tailColor;
                canvas.SetShadow(new SizeF(0, 0), 15, _tailColor);
                var tailPath = new PathF();
                tailPath.MoveTo(-_size * 0.8f, 0);
                tailPath.LineTo(-_size * 1.7f, -_size * 0.7f - tailWave);
                tailPath.LineTo(-_size * 1.5f, 0);
                tailPath.LineTo(-_size * 1.7f, _size * 0.7f + tailWave);
                tailPath.Close();
                canvas.FillPath(tailPath);

                // --- Draw Side Fins ---
                var rightFinPath = new PathF();
                rightFinPath.MoveTo(_size * 0.4f, _size * 0.4f);
                rightFinPath.LineTo(_size * -0.2f, (_size * 0.9f + finFlap) * PECTORAL_FIN_SCALE);
                rightFinPath.LineTo(_size * 0.3f, _size * 0.5f);
                rightFinPath.Close();
                canvas.FillPath(rightFinPath);
                
                var leftFinPath = new PathF();
                leftFinPath.MoveTo(_size * 0.4f, -_size * 0.4f);
                leftFinPath.LineTo(_size * -0.2f, (-_size * 0.9f - finFlap) * PECTORAL_FIN_SCALE);
                leftFinPath.LineTo(_size * 0.3f, -_size * 0.5f);
                leftFinPath.Close();
                canvas.FillPath(leftFinPath);

                // --- Draw Body ---
                canvas.FillColor = _bodyColor;
                canvas.SetShadow(new SizeF(0, 0), FISH_BODY_SHADOW_BLUR, _bodyColor);
                canvas.FillEllipse(
                    -_size * 1.2f / 2, -_size * 0.8f / 2, 
                    _size * 1.2f, _size * 0.8f
                );

                // --- Draw Eyes ---
                canvas.SetShadow(new SizeF(0, 0), 0, Colors.Transparent); // No glow for eyes
                canvas.FillColor = Colors.White;
                float eyeY = _size * 0.35f;
                float eyeX = _size * 0.7f;
                float eyeRadius = _size * 0.1f;
                canvas.FillCircle(eyeX, -eyeY, eyeRadius);
                canvas.FillCircle(eyeX, eyeY, eyeRadius);

                // Pupils
                canvas.FillColor = Colors.Black;
                float pupilRadius = _size * 0.05f;
                canvas.FillCircle(eyeX + (eyeRadius * 0.2f), -eyeY, pupilRadius);
                canvas.FillCircle(eyeX + (eyeRadius * 0.2f), eyeY, pupilRadius);

                canvas.RestoreState();
            }

            private void ChangeState()
            {
                double rand = _random.NextDouble();
                if (rand < 0.7)
                {
                    _state = "swimming";
                    _targetAngle += (float)((_random.NextDouble() - 0.5) * (Math.PI / 2));
                }
                else if (rand < 0.9)
                {
                    _state = "pausing";
                }
                else
                {
                    _state = "darting";
                    _targetAngle += (float)((_random.NextDouble() - 0.5) * Math.PI);
                }
                _stateTimer = GetRandomTimer();
            }

            public void Update(List<Fish> allCreatures, Clock clock, float width, float height)
            {
                if (_x == 0 && _y == 0) Spawn(width, height);

                bool overriddenByInteraction = false;

                // Clock Avoidance
                if (clock != null)
                {
                    float dx = _x - clock.X;
                    float dy = _y - clock.Y;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    float clockRepulsionRadius = (Math.Min(width, height) * 0.08f * 4) + _size;

                    if (distance < clockRepulsionRadius)
                    {
                        _state = "darting";
                        _targetAngle = (float)Math.Atan2(dy, dx);
                        _stateTimer = 50;
                        overriddenByInteraction = true;
                    }
                }

                var neighbors = new List<Fish>();
                float avgX = 0, avgY = 0, avgAngle = 0;

                // Fish Interaction
                if (!overriddenByInteraction)
                {
                    foreach (var other in allCreatures)
                    {
                        if (other == this) continue;
                        float dx = _x - other.X;
                        float dy = _y - other.Y;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        float personalSpace = (_size + other.Size) * 1.5f;

                        if (distance < personalSpace)
                        {
                            _targetAngle = (float)Math.Atan2(dy, dx);
                            if (_bodyColor != other.BodyColor)
                            {
                                _state = "darting";
                                _stateTimer = 50;
                            }
                            else if (_state == "pausing")
                            {
                                _state = "swimming";
                            }
                            overriddenByInteraction = true;
                            break;
                        }

                        const float perceptionRadius = 150;
                        if (other.BodyColor == _bodyColor && distance < perceptionRadius)
                        {
                            neighbors.Add(other);
                            avgX += other.X;
                            avgY += other.Y;
                            avgAngle = LerpAngle(avgAngle, other.Angle, 1f / neighbors.Count);
                        }
                    }
                }
                
                // Schooling Behavior
                if (!overriddenByInteraction && neighbors.Count > 0)
                {
                    _state = "swimming";
                    avgX /= neighbors.Count;
                    avgY /= neighbors.Count;
                    float angleToCenter = (float)Math.Atan2(avgY - _y, avgX - _x);
                    float schoolAngle = LerpAngle(angleToCenter, avgAngle, 0.5f);
                    _targetAngle = LerpAngle(_targetAngle, schoolAngle, 0.02f);
                    overriddenByInteraction = true;
                }

                // Default Wandering Behavior
                if (!overriddenByInteraction)
                {
                    _stateTimer--;
                    if (_stateTimer <= 0) ChangeState();
                }

                // Circular Boundary Avoidance for Watch
                float centerX = width / 2;
                float centerY = height / 2;
                float radius = Math.Min(width, height) / 2 - 5;
                float distanceFromCenter = (float)Math.Sqrt((_x - centerX) * (_x - centerX) + (_y - centerY) * (_y - centerY));
                
                if (distanceFromCenter > radius - 10)
                {
                    _targetAngle = (float)Math.Atan2(centerY - _y, centerX - _x);
                    _state = "swimming";
                    _stateTimer = GetRandomTimer();
                }
                
                float targetSpeed = _speed;
                float turnRate = 0.05f;
                float tailSpeed = 0.2f;

                switch (_state)
                {
                    case "pausing": targetSpeed = _speed * 0.1f; tailSpeed = 0.1f; break;
                    case "darting": targetSpeed = _speed * 3f; turnRate = 0.1f; tailSpeed = 0.5f; break;
                }
                
                _currentSpeed += (targetSpeed - _currentSpeed) * 0.05f;
                _tailAnimation += tailSpeed;
                _angle = LerpAngle(_angle, _targetAngle, turnRate);
                _x += (float)Math.Cos(_angle) * _currentSpeed;
                _y += (float)Math.Sin(_angle) * _currentSpeed;

                // Circular Boundary Clamping
                float maxRadius = Math.Min(width, height) / 2 - _size;
                float distFromCenter = (float)Math.Sqrt((_x - centerX) * (_x - centerX) + (_y - centerY) * (_y - centerY));
                
                if (distFromCenter > maxRadius)
                {
                    float angle = (float)Math.Atan2(_y - centerY, _x - centerX);
                    _x = centerX + (float)Math.Cos(angle) * maxRadius;
                    _y = centerY + (float)Math.Sin(angle) * maxRadius;
                }
            }
        }

        // --- Clock Class ---
        private class Clock
        {
            private float _x, _y, _angle, _speed;
            private readonly Color _colonColor;
            private readonly Color[] _numberColors;
            private readonly Random _random;

            public float X => _x;
            public float Y => _y;

            public Clock(Random random)
            {
                _random = random;
                _angle = (float)(_random.NextDouble() * Math.PI * 2);
                _speed = 0.25f;

                _colonColor = Color.FromHsla(120 / 360f, 1, 0.75f); // Neon Green
                _numberColors = new Color[]
                {
                    Color.FromHsla(180 / 360f, 1, 0.7f), // Neon Cyan
                    Color.FromHsla(300 / 360f, 1, 0.7f), // Neon Magenta
                    Color.FromHsla(60 / 360f, 1, 0.7f),  // Neon Yellow
                    Color.FromHsla(0 / 360f, 1, 0.7f),   // Neon Red
                    Color.FromHsla(240 / 360f, 1, 0.75f),// Neon Blue
                    Color.FromHsla(30 / 360f, 1, 0.7f),  // Neon Orange
                };
            }
            
            public void Spawn(float width, float height)
            {
                 _x = width / 2;
                 _y = height / 4;
            }

            public void Update(float width, float height)
            {
                if (_x == 0 && _y == 0) Spawn(width, height);

                _x += (float)Math.Cos(_angle) * _speed;
                _y += (float)Math.Sin(_angle) * _speed;

                // Wall Bouncing
                float fontSize = Math.Min(width, height) * 0.08f;
                float textWidth = fontSize * 4.8f; // Estimated width for "00:00:00"
                float leftEdge = _x - textWidth / 2;
                float rightEdge = _x + textWidth / 2;
                float topEdge = _y;
                float bottomEdge = _y + fontSize;
                const float margin = 20;

                if ((leftEdge < margin && Math.Cos(_angle) < 0) || (rightEdge > width - margin && Math.Cos(_angle) > 0))
                {
                    _angle = (float)Math.PI - _angle;
                }
                if ((topEdge < margin && Math.Sin(_angle) < 0) || (bottomEdge > height - margin && Math.Sin(_angle) > 0))
                {
                    _angle = -_angle;
                }
            }

            public void Draw(ICanvas canvas, float width, float height)
            {
                DateTime now = DateTime.Now;
                string timeString = now.ToString("HH:mm:ss");

                float fontSize = Math.Min(width, height) * 0.08f;
                var font = new Microsoft.Maui.Graphics.Font("Courier New", (int)fontSize, FontStyleType.Normal);
                
                // Measure the total width to center it properly
                SizeF totalSize = canvas.GetStringSize(timeString, font, fontSize, HorizontalAlignment.Left, VerticalAlignment.Top);
                float currentX = _x - (totalSize.Width / 2);

                int colorIndex = 0;
                foreach (char c in timeString)
                {
                    Color charColor = (c == ':') ? _colonColor : _numberColors[colorIndex % _numberColors.Length];
                    if (c != ':') colorIndex++;
                    
                    canvas.FontColor = charColor;
                    canvas.SetShadow(new SizeF(0, 0), 30, charColor);

                    string character = c.ToString();
                    SizeF charSize = canvas.GetStringSize(character, font, fontSize, HorizontalAlignment.Left, VerticalAlignment.Top);
                    canvas.DrawString(character, currentX, _y, charSize.Width, charSize.Height, HorizontalAlignment.Left, VerticalAlignment.Top);
                    currentX += charSize.Width;
                }
                canvas.SetShadow(new SizeF(0,0), 0, Colors.Transparent);
            }
        }
    }
}