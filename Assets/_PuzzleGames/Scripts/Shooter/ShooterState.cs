public enum ShooterState
{
    None = 0,

    IdleInBottom = 1,
    IdleInMiddle = 2,

    MovingToFlower = 10,
    OnFlower = 11,
    ReturningToMiddle = 12,
    WaitingLinkedReturn = 13,

    Locked = 20,
    Destroyed = 99
}