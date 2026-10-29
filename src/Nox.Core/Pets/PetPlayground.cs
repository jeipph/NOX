namespace Nox.Core.Pets;

/// <summary>
/// La mascota jugando en una franja de suelo: se sienta, persigue el ovillo, le da zarpazos y salta.
/// Todo en píxeles del dibujo (la vista los amplía). No sabe nada de WPF: la ventana llama a
/// <see cref="Step"/> unas veces por segundo solo mientras la pestaña se ve.
/// </summary>
public sealed class PetPlayground
{
    // A ×4 caben en el notch abierto (512 × 144 px).
    public const double StageWidth = 128;
    public const double StageHeight = 36;

    private const double WalkSpeed = 30;      // px/s
    private const double Gravity = 300;       // px/s²
    private const double BallFriction = 1.4;  // cuanto mayor, antes se para el ovillo
    private const double StepPixels = 4;      // cada cuántos px cambia el paso al andar
    private const double MaxStep = 0.1;       // s: un tirón largo no atraviesa paredes

    private enum Activity { Idle, Walk, Crouch, Jump, Swipe }

    private readonly Random _random;
    private Activity _activity = Activity.Idle;
    private double _activityTime;
    private double _activityLength;
    private double _vx, _vy;
    private double _walked;
    private double _blinkIn;
    private double _blinkLeft;
    private bool _pounce;        // el salto termina en zarpazo si cae junto al ovillo
    private double _ballVx, _ballVy;
    private double _ballRolled;

    public PetPlayground(int? seed = null)
    {
        _random = seed is null ? new Random() : new Random(seed.Value);
        PetX = (StageWidth - PetSprites.Width) / 2;
        BallX = StageWidth * 0.75;
        StartIdle();
        _blinkIn = NextDouble(2, 4);
    }

    /// <summary>Borde izquierdo de la mascota.</summary>
    public double PetX { get; private set; }

    /// <summary>Altura sobre el suelo (0 = en el suelo).</summary>
    public double PetY { get; private set; }

    public bool FacingRight { get; private set; } = true;

    public PetPose Pose { get; private set; } = PetPose.Sit;

    /// <summary>Borde izquierdo del ovillo.</summary>
    public double BallX { get; private set; }

    public double BallY { get; private set; }

    public int BallFrame => (int)(_ballRolled / 3) % PetSprites.BallFrameCount;

    /// <summary>Cuántos zarpazos ha dado (para las pruebas).</summary>
    public int Kicks { get; private set; }

    private double PetCenter => PetX + PetSprites.Width / 2.0;

    private double BallCenter => BallX + PetSprites.BallSize / 2.0;

    /// <summary>Avanza el juego <paramref name="seconds"/> segundos.</summary>
    public void Step(double seconds)
    {
        while (seconds > 0)
        {
            double dt = Math.Min(seconds, MaxStep);
            seconds -= dt;
            UpdateBall(dt);
            UpdatePet(dt);
        }
    }

    private void UpdateBall(double dt)
    {
        BallX += _ballVx * dt;
        _ballRolled += Math.Abs(_ballVx * dt);
        _ballVx *= Math.Exp(-BallFriction * dt);
        if (Math.Abs(_ballVx) < 2)
            _ballVx = 0;

        double right = StageWidth - PetSprites.BallSize;
        if (BallX < 0 || BallX > right)
        {
            BallX = Math.Clamp(BallX, 0, right);
            _ballVx = -_ballVx * 0.6; // rebota en la pared
        }

        if (BallY > 0 || _ballVy > 0)
        {
            _ballVy -= Gravity * dt;
            BallY += _ballVy * dt;
            if (BallY <= 0)
            {
                BallY = 0;
                _ballVy = _ballVy < -40 ? -_ballVy * 0.4 : 0; // botecito
            }
        }
    }

    private void UpdatePet(double dt)
    {
        _activityTime += dt;
        switch (_activity)
        {
            case Activity.Idle:
                Pose = Blinking(dt) ? PetPose.SitBlink : PetPose.Sit;
                if (_activityTime >= _activityLength)
                    ChooseNext();
                break;

            case Activity.Walk:
                FacingRight = BallCenter > PetCenter;
                double gap = FacingRight ? BallX - (PetX + PetSprites.Width) : PetX - (BallX + PetSprites.BallSize);
                if (gap <= 1.5 && BallY < 4)
                {
                    StartSwipe();
                    break;
                }
                // Si el ovillo rueda lejos durante mucho rato, se lanza a por él de un salto.
                if (_activityTime > 3 && gap > 20)
                {
                    StartCrouch(pounce: true);
                    break;
                }
                double move = Math.Min(WalkSpeed * dt, Math.Max(gap - 1, 0));
                PetX = ClampPet(PetX + (FacingRight ? move : -move));
                _walked += move;
                Pose = (int)(_walked / StepPixels) % 2 == 0 ? PetPose.StandA : PetPose.StandB;
                break;

            case Activity.Crouch:
                Pose = PetPose.Crouch;
                if (_activityTime >= _activityLength)
                {
                    _activity = Activity.Jump;
                    _activityTime = 0;
                    _vy = NextDouble(80, 108); // como mucho ~19 px de alto: no se sale por arriba
                    _vx = _pounce ? (FacingRight ? 45 : -45) : NextDouble(-10, 10);
                }
                break;

            case Activity.Jump:
                Pose = PetPose.Jump;
                _vy -= Gravity * dt;
                PetY += _vy * dt;
                double x = PetX + _vx * dt;
                PetX = ClampPet(x);
                if (PetX != x)
                    _vx = 0; // chocó con la pared
                if (PetY <= 0)
                {
                    PetY = 0;
                    double reach = FacingRight ? BallX - (PetX + PetSprites.Width) : PetX - (BallX + PetSprites.BallSize);
                    if (_pounce && reach < 6 && reach > -PetSprites.Width)
                        StartSwipe();
                    else
                        StartIdle();
                }
                break;

            case Activity.Swipe:
                Pose = _activityTime < 0.25 ? PetPose.Swipe : PetPose.StandA;
                if (_activityTime >= _activityLength)
                {
                    if (_random.NextDouble() < 0.35)
                        StartCrouch(pounce: true); // salta detrás del ovillo
                    else
                        StartIdle();
                }
                break;
        }
    }

    private void ChooseNext()
    {
        double choice = _random.NextDouble();
        if (choice < 0.55)
        {
            _activity = Activity.Walk;
            _activityTime = 0;
        }
        else if (choice < 0.85)
        {
            FacingRight = _random.NextDouble() < 0.5;
            StartCrouch(pounce: false); // salto de alegría
        }
        else
        {
            StartIdle(); // se queda un rato más sentado
        }
    }

    private void StartIdle()
    {
        _activity = Activity.Idle;
        _activityTime = 0;
        _activityLength = NextDouble(1.2, 3.2);
        _vx = _vy = 0;
    }

    private void StartCrouch(bool pounce)
    {
        _activity = Activity.Crouch;
        _activityTime = 0;
        _activityLength = 0.3;
        _pounce = pounce;
        if (pounce)
            FacingRight = BallCenter > PetCenter;
    }

    private void StartSwipe()
    {
        _activity = Activity.Swipe;
        _activityTime = 0;
        _activityLength = 0.6;
        Kicks++;
        double direction = FacingRight ? 1 : -1;
        _ballVx = direction * NextDouble(55, 115);
        _ballVy = _random.NextDouble() < 0.4 ? NextDouble(40, 90) : 0; // a veces sale volando un poco
    }

    // Parpadea un instante cada pocos segundos.
    private bool Blinking(double dt)
    {
        if (_blinkLeft > 0)
        {
            _blinkLeft -= dt;
            return true;
        }
        _blinkIn -= dt;
        if (_blinkIn > 0)
            return false;
        _blinkIn = NextDouble(2, 5);
        _blinkLeft = 0.15;
        return true;
    }

    private static double ClampPet(double x) => Math.Clamp(x, 0, StageWidth - PetSprites.Width);

    private double NextDouble(double min, double max) => min + _random.NextDouble() * (max - min);
}
