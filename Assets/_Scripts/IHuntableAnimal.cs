public interface IHuntableAnimal
{
    public AnimalType AnimalType { get; }
    public int Points { get; }
    
    public bool TryHunt();
}