namespace custom_router.validation.api;

public interface IValidator<in TValue>
{
    bool Validate(TValue value);
}