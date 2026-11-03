namespace Nox.Core.Pets;

/// <summary>
/// La mascota jugando en una franja de suelo: se sienta, se lame, se estira, duerme, persigue el
/// juguete, le da zarpazos, salta, maúlla o ladra, y come de su cuenco. Todo en píxeles del dibujo (la vista los amplía). No sabe nada
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
    private const double TalkLength = 1.4;    // s que dura un «¡Miau!»
    private const double EatLength = 4;       // s comiendo
    private const double EmptyBowlStays = 3;  // s que se ve el cuenco vacío antes de desaparecer

    private enum Activity { Idle, Walk, Crouch, Jump, Swipe, Groom, Stretch, Sleep, Talk, GoEat, Eat }

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
    private double _eatTargetX;
    private bool _eatFacingRight;
    private double _emptyBowlLeft;
    private int _poops;

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

    /// <summary>Maullando o ladrando ahora mismo.</summary>
    public bool IsTalking => _activity == Activity.Talk;

    /// <summary>Comiendo del cuenco (con la cabeza baja).</summary>
    public bool IsEating => _activity == Activity.Eat;

    /// <summary>Cuántas veces ha maullado o ladrado (para las pruebas).</summary>
    public int Talks { get; private set; }

    /// <summary>
    /// Tiene hambre (lo decide <see cref="PetCare"/>): muestra «¡Tengo hambre!», habla más y no se duerme.
    /// </summary>
    public bool IsHungry { get; set; }

    /// <summary>Qué bocadillo se ve sobre su cabeza.</summary>
    public PetBubble Bubble => _activity switch
    {
        Activity.GoEat or Activity.Eat => PetBubble.None,
        _ when IsHungry => PetBubble.Hungry,
        Activity.Talk => PetBubble.Talk,
        _ => PetBubble.None,
    };

    /// <summary>Hay un cuenco en el suelo.</summary>
    public bool HasBowl { get; private set; }

    /// <summary>El cuenco aún tiene comida (si no, está vacío y pronto desaparece).</summary>
    public bool BowlFull { get; private set; }

    /// <summary>Borde izquierdo del cuenco.</summary>
    public double BowlX { get; private set; }

    /// <summary>Popós en el suelo (de 0 a <see cref="PetCare.MaxPoops"/>).</summary>
    public int Poops
    {
        get => _poops;
        set => _poops = Math.Clamp(value, 0, PetCare.MaxPoops);
    }

    /// <summary>
    /// Borde izquierdo del popó número <paramref name="index"/>: sitios fijos repartidos por el suelo,
    /// para que no se amontonen ni bailen al redibujar.
    /// </summary>
    public double PoopX(int index)
    {
        double[] spots = [0.1, 0.52, 0.86];
        return Math.Round(spots[Math.Clamp(index, 0, spots.Length - 1)] * (StageWidth - PetSprites.PoopWidth));
    }

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
            UpdateBowl(dt);
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
        if (_sincePoke < PokeCooldown || IsBusy)
            return;
        _sincePoke = 0;
        FacingRight = x > PetCenter;
        double choice = _random.NextDouble();
        if (_activity == Activity.Sleep || choice < 0.45)
            StartCrouch(pounce: false); // salto de sorpresa o de alegría
        else if (choice < 0.75)
            StartTalk(); // te saluda
        else
            StartIdle();
    }

    /// <summary>Clic sobre la mascota: se gira hacia el ratón y maúlla o ladra (si no está ocupada).</summary>
    public void Touch(double x)
    {
        if (IsBusy || _activity == Activity.Talk)
            return;
        _sincePoke = 0;
        FacingRight = x > PetCenter;
        StartTalk();
    }

    /// <summary>
    /// «Dar comida»: aparece un cuenco lleno lejos de la mascota; ella va hasta él y come unos segundos.
    /// Si está en el aire, va al caer.
    /// </summary>
    public void PlaceFood()
    {
        double center = PetCenter < StageWidth / 2 ? StageWidth * 0.72 : StageWidth * 0.28;
        BowlX = Math.Round(Math.Clamp(center - PetSprites.BowlWidth / 2.0, 0, StageWidth - PetSprites.BowlWidth));
        HasBowl = true;
        BowlFull = true;
        _emptyBowlLeft = 0;
        if (_activity is not (Activity.Jump or Activity.Crouch))
            StartGoEat();
    }

    // Saltando o comiendo no reacciona al ratón.
    private bool IsBusy => _activity is Activity.Jump or Activity.Crouch or Activity.GoEat or Activity.Eat;

    private double BowlCenter => BowlX + PetSprites.BowlWidth / 2.0;

    private void UpdateBowl(double dt)
    {
        if (!HasBowl || BowlFull || _activity == Activity.Eat)
            return;
        _emptyBowlLeft -= dt;
        if (_emptyBowlLeft <= 0)
            HasBowl = false;
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

            case Activity.Talk:
                // Dos golpes de boca: «mi-au» o «guau, guau».
                Pose = _activityTime < 0.45 || (_activityTime >= 0.6 && _activityTime < 0.9) ? PetPose.Talk : PetPose.Sit;
                if (_activityTime >= _activityLength)
                    StartIdle();
                break;

            case Activity.GoEat:
                double distance = _eatTargetX - PetX;
                if (Math.Abs(distance) <= 0.5)
                {
                    PetX = _eatTargetX;
                    FacingRight = _eatFacingRight;
                    Start(Activity.Eat, EatLength);
                    Pose = PetPose.EatA;
                    break;
                }
                FacingRight = distance > 0;
                double stride = Math.Min(WalkSpeed * dt, Math.Abs(distance));
                PetX = ClampPet(PetX + Math.Sign(distance) * stride);
                _walked += stride;
                Pose = (int)(_walked / StepPixels) % 2 == 0 ? PetPose.StandA : PetPose.StandB;
                break;

            case Activity.Eat:
                Pose = (int)(_activityTime / 0.4) % 2 == 0 ? PetPose.EatA : PetPose.EatB;
                if (_activityTime >= _activityLength)
                {
                    BowlFull = false;
                    _emptyBowlLeft = EmptyBowlStays;
                    StartIdle();
                }
                break;
        }
    }

    // Se pone de forma que la cabeza baja quede justo sobre el cuenco, mirando a un lado o al otro
    // (el que le pille más cerca y quepa en el escenario).
    private void StartGoEat()
    {
        double right = ClampPet(BowlCenter - PetSprites.EatingHeadCenter);
        double left = ClampPet(BowlCenter - (PetSprites.Width - PetSprites.EatingHeadCenter));
        bool useRight = Math.Abs(right - PetX) <= Math.Abs(left - PetX);
        _eatTargetX = useRight ? right : left;
        _eatFacingRight = useRight;
        Start(Activity.GoEat, 0);
    }

    private void StartTalk()
    {
        Start(Activity.Talk, TalkLength);
        Pose = PetPose.Talk;
        Talks++;
    }

    private void ChooseNext()
    {
        // Con hambre se queja a menudo y no se duerme.
        if (IsHungry && _random.NextDouble() < 0.3)
        {
            StartTalk();
            return;
        }
        if (_random.NextDouble() < 0.08)
        {
            StartTalk(); // a veces maúlla o ladra porque sí
            return;
        }

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
        else if (choice < 0.93 && !IsHungry)
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

    private void StartIdle()
    {
        if (HasBowl && BowlFull)
            StartGoEat(); // le pusieron comida mientras saltaba
        else
            Start(Activity.Idle, NextDouble(1.2, 3.2));
    }

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
