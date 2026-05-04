namespace AgentSapienxa.Domain.Agents;

public sealed record AgentKey(string Value)
{
    public static readonly AgentKey CursosIntent = new("cursos_intent");
    public static readonly AgentKey CursosGeneral = new("cursos_general");
    public static readonly AgentKey CursosPagos = new("cursos_pagos");

    public override string ToString() => Value;
}
