using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQRS.Todo.Shared.Domain.Bus.Commands;

namespace CQRS.Todo.Shared.Infrastructure.Bus.Commands;

public class InMemoryCommandBus : CommandBus
{
    private readonly IServiceProvider _provider;
    private static readonly ConcurrentDictionary<Type, IEnumerable<CommandHandlerWrapper>> _commandHandlers = new();

    public InMemoryCommandBus(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task Dispatch(Command command)
    {
        var wrappedHandlers = GetWrappedHandlers(command);
            
        if(wrappedHandlers == null) throw new CommandNotRegisteredError(command);
            
        foreach (CommandHandlerWrapper handler in wrappedHandlers)
        {
            await handler.Handle(command, _provider);
        }
    }

    private IEnumerable<CommandHandlerWrapper> GetWrappedHandlers(Command command)
    {
        Type handlerType = typeof(CommandHandler<>).MakeGenericType(command.GetType());
        Type wrapperType = typeof(CommandHandlerWrapper<>).MakeGenericType(command.GetType());

        IEnumerable handlers =
            (IEnumerable) _provider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType));

        var wrappedHandlers = _commandHandlers.GetOrAdd(command.GetType(), handlers.Cast<object>()
            .Select(_ => (CommandHandlerWrapper) Activator.CreateInstance(wrapperType)));
            
        return wrappedHandlers;
    }
}