/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *     
 *              https://www.firebirdsql.org/en/net-provider/ 
 *              
 *     Permission to use, copy, modify, and distribute this software and its
 *     documentation for any purpose, without fee, and without a written
 *     agreement is hereby granted, provided that the above copyright notice
 *     and this paragraph and the following two paragraphs appear in all copies. 
 * 
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *      Credits: Rafael Almeida (ralms@ralms.net)
 *                              Sergipe-Brazil
 *
 *
 *                              
 *                  All Rights Reserved.
 */

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Query.Expressions.Internal
{

    public class FbSubStringExpression : Expression
    {

        public FbSubStringExpression([NotNull] Expression subjectExpression, [NotNull] Expression fromExpression, [NotNull] Expression forExpression)
        {
            Check.NotNull(subjectExpression, nameof(subjectExpression));
            Check.NotNull(fromExpression, nameof(fromExpression));
            Check.NotNull(forExpression, nameof(forExpression));

            SubjectExpression = subjectExpression;
            FromExpression = fromExpression;
            ForExpression = forExpression;
        }
        public virtual Expression SubjectExpression { get; }

        public virtual Expression FromExpression { get; }

        public virtual Expression ForExpression { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(string);

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            Check.NotNull(visitor, nameof(visitor));

            var specificVisitor = visitor as IFbExpressionVisitor;

            return specificVisitor != null
                ? specificVisitor.VisitSubString(this)
                : base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newSubjectExpression = visitor.Visit(SubjectExpression);
            var newFromExpression = visitor.Visit(FromExpression);
            var newForExpression = visitor.Visit(ForExpression);

            return newFromExpression != FromExpression
                   || newForExpression != ForExpression
                   || newSubjectExpression != SubjectExpression
                ? new FbSubStringExpression(newSubjectExpression, newFromExpression, newForExpression)
                : this;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false; 

            if (ReferenceEquals(this, obj)) 
                return true; 

            return obj.GetType() == GetType() && Equals((FbSubStringExpression)obj);
        }

        private bool Equals(FbSubStringExpression other)
            => Equals(FromExpression, other.FromExpression)
               && Equals(ForExpression, other.ForExpression)
               && Equals(SubjectExpression, other.SubjectExpression);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SubjectExpression.GetHashCode();
                hashCode = (hashCode * 397) ^ FromExpression.GetHashCode();
                hashCode = (hashCode * 397) ^ ForExpression.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() => $"SUBSTRING({SubjectExpression} FROM {FromExpression} FOR {ForExpression})";

    }

}