﻿namespace IBlang.Stage3TypeChecker;

using IBlang.Stage2Parser;

public class TypeChecker
{
    private readonly Context ctx;

    public TypeChecker(Context ctx)
    {
        this.ctx = ctx;
    }

    public Ast TypeCheck(Ast ast)
    {
        return ast;
    }
}
