using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ShipRate.Infrastructure.Filters;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

public class DeleteService<TDbContext> where TDbContext : DbContext
{
    private readonly TDbContext _context;

    public DeleteService(TDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteResult> AutoSoftDeleteAsync<TEntity>(object id, Guid userId)
        where TEntity : class
    {
        if (id == null)
            return DeleteResult.Fail("شناسه رکورد نامعتبر است.");

        var entityType = _context.Model.FindEntityType(typeof(TEntity));
        if (entityType == null)
            return DeleteResult.Fail("این انتیتی در DbContext ثبت نشده است.");

        var isDeletedProperty = entityType.FindProperty("IsDeleted");
        if (isDeletedProperty?.ClrType != typeof(bool))
            return DeleteResult.Fail("پراپرتی IsDeleted از نوع bool یافت نشد.");

        // برای کلید اصلی مرکب نیز می‌توان object[] حاوی مقادیر کلید ارسال کرد.
        object[] keyValues = id is object[] values ? values : new[] { id };

        var entity = await _context.Set<TEntity>().FindAsync(keyValues);
        if (entity == null)
            return DeleteResult.Fail("رکورد مورد نظر یافت نشد.");

        var entry = _context.Entry(entity);

        if ((bool)entry.Property("IsDeleted").CurrentValue!)
            return DeleteResult.Fail("این رکورد قبلاً حذف شده است.");

        foreach (var fk in entityType.GetReferencingForeignKeys())
        {
            var dependentType = fk.DeclaringEntityType;
            var childIsDeleted = dependentType.FindProperty("IsDeleted");

            // وابستگی ناشناخته را بی‌صدا نادیده نگیریم.
            if (childIsDeleted?.ClrType != typeof(bool))
                return DeleteResult.Fail(
                    $"امکان بررسی وابستگی «{dependentType.ClrType.Name}» وجود ندارد: " +
                    "IsDeleted از نوع bool یافت نشد.");

            // FK ممکن است به کلید جایگزین یا کلید مرکب اشاره کند، نه لزوماً id ورودی.
            var principalValues = fk.PrincipalKey.Properties
                .Select(p => entry.Property(p.Name).CurrentValue!)
                .ToArray();

            var checkMethod = typeof(DeleteService<TDbContext>)
                .GetMethod(
                    nameof(CheckActiveDependentsAsync),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            var task = (Task<bool>)checkMethod
                .MakeGenericMethod(dependentType.ClrType)
                .Invoke(this, new object[] { fk, principalValues, childIsDeleted })!;

            if (await task)
            {
                var displayName =
                    dependentType.ClrType.GetCustomAttribute<DisplayNameAttribute>()
                        ?.DisplayName
                    ?? dependentType.ClrType.GetCustomAttribute<DisplayAttribute>()
                        ?.GetName()
                    ?? dependentType.ClrType.Name;

                return DeleteResult.Fail(
                    $"Deletion is not allowed. Active records exist in {displayName}.");
            }
        }

        entry.Property("IsDeleted").CurrentValue = true;
        //
        //entry.Property("DeletedAt").CurrentValue = DateTime.Now;
        //entry.Property("DeleteUserId").CurrentValue = userId;
        entry.Property("IsDeleted").CurrentValue = true;

        // هر دو ستون حذف-نرم
        var deletedAtProp = entityType.FindProperty("DeletedAt");
        var deleteUserIdProp = entityType.FindProperty("DeleteUserId");

        // ستون‌های جایگزین (ویرایش)
        var editedAtProp = entityType.FindProperty("EditedAt");
        var editUserIdProp = entityType.FindProperty("EditUserId");

        // ۱) اگر ستون‌های DeletedAt و DeleteUserId وجود دارند، جلویشان مقدار بگذار
        if (deletedAtProp != null)
            entry.Property("DeletedAt").CurrentValue = DateTime.Now;

        if (deleteUserIdProp != null)
            entry.Property("DeleteUserId").CurrentValue = userId;

        // ۲) اگر نبودند (یعنی جدول Edit می‌خواند)، برو سراغ EditedAt و EditUserId
        else if (editedAtProp != null)
            entry.Property("UpdatedAt").CurrentValue = DateTime.Now;

        if (deleteUserIdProp == null && editUserIdProp != null)
            entry.Property("EditUserId").CurrentValue = userId;

        await _context.SaveChangesAsync();
        return DeleteResult.Ok();

    }

    private async Task<bool> CheckActiveDependentsAsync<TDependent>(IForeignKey fk, object[] principalValues, IProperty childIsDeleted) where TDependent : class
    {
        var x = Expression.Parameter(typeof(TDependent), "x");
        var efPropertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!;

        Expression condition = Expression.Constant(true);

        for (int i = 0; i < fk.Properties.Count; i++)
        {
            var fkProperty = fk.Properties[i];

            // EF.Property به پراپرتی‌های معمولی و Shadow دسترسی می‌دهد.
            var childFk = Expression.Call(
                efPropertyMethod.MakeGenericMethod(fkProperty.ClrType),
                x,
                Expression.Constant(fkProperty.Name));

            Expression parentValue = Expression.Constant(principalValues[i]);

            // مثلاً Guid والد را برای مقایسه با Guid? فرزند تبدیل می‌کند.
            if (parentValue.Type != fkProperty.ClrType)
                parentValue = Expression.Convert(
                    parentValue, fkProperty.ClrType);

            condition = Expression.AndAlso(
                condition,
                Expression.Equal(childFk, parentValue));
        }

        var isDeleted = Expression.Call(
            efPropertyMethod.MakeGenericMethod(typeof(bool)),
            x,
            Expression.Constant(childIsDeleted.Name));

        condition = Expression.AndAlso(condition, Expression.Not(isDeleted));

        var predicate = Expression.Lambda<Func<TDependent, bool>>(condition, x);

        return await _context.Set<TDependent>().AnyAsync(predicate);
    }
}
