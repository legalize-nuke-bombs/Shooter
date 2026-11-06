namespace Shooter.Infrastructure
{
    public readonly struct ComponentSave
    {
        public readonly SaveableEntity Entity;
        public readonly ISaveableComponent Component;
        public readonly SaveToken Content;

        public ComponentSave(SaveableEntity entity, ISaveableComponent component, SaveToken content)
        {
            Entity = entity;
            Component = component;
            Content = content;
        }
    }
}
