namespace Nox.Core.Pets;

/// <summary>
/// La mascota jugando en una franja de suelo: se sienta, se lame, se estira, duerme, persigue el
/// juguete, le da zarpazos y salta. Todo en píxeles del dibujo (la vista los amplía). No sabe nada
/// de WPF: la ventana llama a <see cref="Step"/> unas veces por segundo solo mientras se ve.
/// </summary>
public sealed class PetPlayground
{
    // A ×4 caben en el notch abierto (512 × 144 px).
    public const double DefaultStageWidth = 128;
    public const double StageHeight = 36;

    private const double WalkSpeed = 30;      // px/s
    private const double Gravity = 300;       // px/s²
    private const double StepPixels = 4;      // cada cuántos px cambia el paso al andar
    private const double MaxStep = 0.1;       // s: un tirón largo no atraviesa paredes
    private const double PokeCooldown = 2;    // s entre reacciones al ratón

    private enum Activity { Idle, Walk, Crouch, Jump, Swipe, Groom, Stretch, Sleep }

    private readonly Random _random;
    private Activity _activity = Activity.Idle;
    private double _activityTime;
    private double _activityLength;
    private double _vx, _vy;
    private double _walked;
    private double _blinkIn;
    private double _blinkLeft;
    private bool _pounce;        // el salto termina en zarpazo si cae junto al juguete
    private double _toyVx, _toyVy;
    private double _toyRolled;
    private double _mouseRestLeft; // ratón de juguete: tiempo hasta que se escape solo
    private double _sincePoke = PokeCooldown;

    public PetPlayground(int? seed = null, double stageWidth = DefaultStageWidth)
    {
        _random = seed is null ? new Random() : new Random(seed.Value);
        StageWidth = Math.Max(stageWidth, PetSprites.Width * 3);
        PetX = (StageWidth - PetSprites.Width) / 2;
        ToyX = StageWidth * 0.75;
        StartIdle();
        _blinkIn = NextDouble(2, 4);
        _mouseRestLeft = NextDouble(2, 5);
    }

    public double StageWidth { get; }

    public PetToy Toy { get; set; } = PetToy.Yarn;

    /// <summary>Borde izquierdo de la mascota.</summary>
    public double PetX { get; private set; }

    /// <summary>Altura sobre el suelo (0 = en el suelo).</summary>
    public double PetY { get; private set; }

    public bool FacingRight { get; private set; } = true;

    public PetPose Pose { get; private set; } = PetPose.Sit;

    /// <summary>Borde izquierdo del juguete.</summary>
    public double ToyX { get; private set; }

    public double ToyY { get; private set; }

    /// <summary>Hacia dónde mira el juguete (solo se nota en el ratón).</summary>
    public bool ToyFacingRight { get; private set; } = true;

    public int ToyFrame => (int)(_toyRolled / 3) % PetSprites.ToyFrameCount(Toy);

    /// <summary>Cuántos zarpazos ha dado (para las pruebas).</summary>
    public int Kicks { get; private set; }

    public bool IsSleeping => _activity == Activity.Sleep;

    private double PetCenter => PetX + PetSprites.Width / 2.0;

    private double ToyCenter => ToyX + PetSprites.ToySize / 2.0;

    /// <summary>Avanza el juego <paramref name="seconds"/> segundos.</summary>
    public void Step(double seconds)
    {
        while (seconds > 0)
        {
            double dt = Math.Min(seconds, MaxStep);
            seconds -= dt;
            _sincePoke += dt;
            UpdateToy(dt);
            UpdatePet(dt);
        }
    }

    /// <summary>
    /// El ratón pasó cerca (posición en píxeles del dibujo): la mascota mira hacia allí y, si
    /// dormía, se despierta de un salto. Como mucho una vez cada 2 s.
    /// </summary>
    public void Poke(double x)
    {
        if (_sincePoke < PokeCooldown || _activity is Activity.Jump or Activity.Crouch)
            return;
        _sincePoke = 0;
        FacingRight = x > PetCenter;
        if (_activity == Activity.Sleep || _random.NextDouble() < 0.6)
            StartCrouch(pounce: false); // salto de sorpresa o de alegría
        else
            StartIdle();
    }

    private void UpdateToy(double dt)
    {
        // La pelota rueda lejos y bota; el ovillo y el ratón frenan antes.
        double friction = Toy == PetToy.Ball ? 0.7 : 1.4;
        double bounce = Toy == PetToy.Ball ? 0.6 : 0.4;

        if (Toy == PetToy.Mouse && _toyVx == 0 && ToyY == 0 && _activity != Activity.Sleep)
        {
            // El ratón de juguete se escapa solo de vez en cuando, lejos de la mascota.
            _mouseRestLeft -= dt;
            if (_mouseRestLeft <= 0)
            {
                _mouseRestLeft = NextDouble(2.5, 6);
                double away = ToyCenter >= PetCenter ? 1 : -1;
                if (ToyX < 10) away = 1;
                if (ToyX > StageWidth - PetSprites.ToySize - 10) away = -1;
                _toyVx = away * NextDouble(45, 80);
            }
        }

        ToyX += _toyVx * dt;
        _toyRolled += Math.Abs(_toyVx * dt);
        if (_toyVx != 0)
            ToyFacingRight = _toyVx > 0;
        _toyVx *= Math.Exp(-friction * dt);
        if (Math.Abs(_toyVx) < 2)
            _toyVx = 0;

        double right = StageWidth - PetSprites.ToySize;
        if (ToyX < 0 || ToyX > right)
        {
            ToyX = Math.Clamp(ToyX, 0, right);
            _toyVx = -_toyVx * 0.6; // rebota en la pared
        }

        if (ToyY > 0 || _toyVy > 0)
        {
            _toyVy -= Gravity * dt;
            ToyY = Math.Min(ToyY + _toyVy * dt, StageHeight - PetSprites.ToySize);
            if (ToyY <= 0)
            {
                ToyY = 0;
                _toyVy = _toyVy < -40 ? -_toyVy * bounce : 0; // bote
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
                FacingRight = ToyCenter > PetCenter;
                double gap = FacingRight ? ToyX - (PetX + PetSprites.Width) : PetX - (ToyX + PetSprites.ToySize);
                if (gap <= 1.5 && ToyY < 4)
                {
                    StartSwipe();
                    break;
                }
                // Si el juguete se va lejos durante mucho rato, se lanza a por él de un salto.
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
                    double reach = FacingRight ? ToyX - (PetX + PetSprites.Width) : PetX - (ToyX + PetSprites.ToySize);
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
                        StartCrouch(pounce: true); // salta detrás del juguete
                    else
                        StartIdle();
                }
                break;

            case Activity.Groom:
                Pose = (int)(_activityTime / 0.3) % 2 == 0 ? PetPose.LickA : PetPose.LickB;
                if (_activityTime >= _activityLength)
                    StartIdle();
                break;

            case Activity.Stretch:
                Pose = PetPose.Stretch;
                if (_activityTime >= _activityLength)
                    StartIdle();
                break;

            case Activity.Sleep:
                Pose = (int)(_activityTime / 1.0) % 2 == 0 ? PetPose.SleepA : PetPose.SleepB;
                if (_activityTime >= _activityLength)
                    Start(Activity.Stretch, 1.2); // al despertar se estira
                break;
        }
    }

    private void ChooseNext()
    {
        double choice = _random.NextDouble();
        if (choice < 0.45)
            Start(Activity.Walk, 0);
        else if (choice < 0.65)
        {
            FacingRight = _random.NextDouble() < 0.5;
            StartCrouch(pounce: false); // salto de alegría
        }
        else if (choice < 0.77)
            Start(Activity.Groom, NextDouble(1.8, 3.2));
        else if (choice < 0.86)
            Start(Activity.Stretch, 1.2);
        else if (choice < 0.93)
            Start(Activity.Sleep, NextDouble(6, 12));
        else
            StartIdle(); // se queda un rato más sentado
    }

    private void Start(Activity activity, double length)
    {
        _activity = activity;
        _activityTime = 0;
        _activityLength = length;
        _vx = _vy = 0;
    }

    private void StartIdle() => Start(Activity.Idle, NextDouble(1.2, 3.2));

    private void StartCrouch(bool pounce)
    {
        Start(Activity.Crouch, 0.3);
        _pounce = pounce;
        if (pounce)
            FacingRight = ToyCenter > PetCenter;
    }

    private void StartSwipe()
    {
        Start(Activity.Swipe, 0.6);
        Kicks++;
        double direction = FacingRight ? 1 : -1;
        _toyVx = direction * NextDouble(55, 115);
        // La pelota casi siempre sale botando; el ovillo y el ratón, a veces.
        double hop = Toy == PetToy.Ball ? 0.85 : 0.4;
        _toyVy = _random.NextDouble() < hop ? NextDouble(40, 90) : 0;
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

    private double ClampPet(double x) => Math.Clamp(x, 0, StageWidth - PetSprites.Width);

    private double NextDouble(double min, double max) => min + _random.NextDouble() * (max - min);
}
