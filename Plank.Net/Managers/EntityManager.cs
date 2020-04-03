﻿using Microsoft.Practices.EnterpriseLibrary.Validation;
using Plank.Net.Contracts;
using Plank.Net.Data;
using Plank.Net.Models;
using Plank.Net.Profiles;
using Plank.Net.Utilities;
using Serialize.Linq.Serializers;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Plank.Net.Managers
{
    public sealed class EntityManager<TEntity> : IEntityManager<TEntity> where TEntity : class, IEntity
    {
        #region MEMBERS

        private readonly IEntityRepository<TEntity> _repository;
        private readonly ILogger<TEntity> _logger;

        #endregion

        #region CONSTRUCTORS

        public EntityManager(DbContext context)
            : this(new EntityRepository<TEntity>(context), new EntityLogger<TEntity>())
        {

        }

        public EntityManager(IEntityRepository<TEntity> repository, ILogger<TEntity> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger     = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region METHODS

        public async Task<PlankPostResponse<TEntity>> CreateAsync(TEntity entity)
        {
            _logger.Info(entity.ToJson());

            var validation = ValidateEntity(entity);

            if (validation.IsValid)
            {
                try
                {
                    await _repository.CreateAsync(entity);
                }
                catch (DataException e)
                {
                    _logger.Error(e);

                    var msg = "There was an issue processing the request, please try again";
                    var valresult = new ValidationResult(msg, this, "Error", null, null);

                    validation.AddResult(valresult);
                }
            }

            var results = new PlankPostResponse<TEntity> { Item = entity, ValidationResults = Mapping<TEntity>.Mapper.Map<PlankValidationResults>(validation) };
            _logger.Info(results.ToJson());

            return results;
        }

        public async Task<PlankDeleteResponse> DeleteAsync(int id)
        {
            _logger.Info(id);

            var validation = new ValidationResults();

            try
            {
                if (await _repository.GetAsync(id) != null)
                {
                    await _repository.DeleteAsync(id);
                }
            }
            catch (DataException e)
            {
                _logger.Error(e);

                var msg = "There was an issue processing the request, please try again";
                var valresult = new ValidationResult(msg, this, "Error", null, null);

                validation.AddResult(valresult);
            }

            var results = new PlankDeleteResponse { Id = id, ValidationResults = Mapping<TEntity>.Mapper.Map<PlankValidationResults>(validation) };

            _logger.Info(results.ToJson());
            return results;
        }

        public async Task<PlankGetResponse<TEntity>> GetAsync(int id)
        {
            _logger.Info(id);
            PlankGetResponse<TEntity> result;

            try
            {
                var item = await _repository.GetAsync(id);
                result = new PlankGetResponse<TEntity>(item)
                {
                    IsValid = true
                };
            }
            catch (DataException e)
            {
                _logger.Error(e);
                result = new PlankGetResponse<TEntity>()
                {
                    IsValid = false,
                    Message = "There was an issue processing the request, please try again"
                };
            }

            _logger.Info(result.ToJson());
            return result;
        }

        public async Task<PlankEnumerableResponse<TEntity>> SearchAsync(Expression<Func<TEntity, bool>> expression, int pageNumber, int pageSize)
        {
            expression = expression ?? (f => true);

            var serializer = new ExpressionSerializer(new JsonSerializer());
            _logger.Info(serializer.SerializeText(expression));

            PlankEnumerableResponse<TEntity> result = null;
            try
            {
                var pagedList  = await _repository.SearchAsync(expression, pageNumber, pageSize);
                result         = Mapping<TEntity>.Mapper.Map<PlankEnumerableResponse<TEntity>>(pagedList);
                result.IsValid = true;
            }
            catch(DataException e)
            {
                _logger.Error(e);
                result = new PlankEnumerableResponse<TEntity>()
                {
                    IsValid = false,
                    Message = "There was an issue processing the request, please try again"
                };
            }

            _logger.Info(result.ToJson());
            return result;
        }

        public async Task<PlankPostResponse<TEntity>> UpdateAsync(TEntity entity)
        {
            _logger.Info(entity.ToJson());

            var validation = ValidateEntity(entity);

            if (validation.IsValid)
            {
                try
                {
                    if (await _repository.GetAsync(entity.Id) != null)
                    {
                        await _repository.UpdateAsync(entity);
                    }
                    else
                    {
                        var msg = "Item could not be found.";
                        var valresult = new ValidationResult(msg, this, "Error", null, null);

                        validation.AddResult(valresult);
                    }
                }
                catch (DataException e)
                {
                    _logger.Error(e);

                    var msg = "There was an issue processing the request, please try again";
                    var valresult = new ValidationResult(msg, this, "Error", null, null);

                    validation.AddResult(valresult);
                }
            }

            var results = new PlankPostResponse<TEntity> { Item = entity, ValidationResults = Mapping<TEntity>.Mapper.Map<PlankValidationResults>(validation) };
            _logger.Info(results.ToJson());

            return results;
        }

        public async Task<PlankPostResponse<TEntity>> UpdateAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
        {
            _logger.Info(entity.ToJson());

            var validation = new ValidationResults();

            try
            {
                if (await _repository.GetAsync(entity.Id) != null)
                {
                    await _repository.UpdateAsync(entity, properties);
                }
                else
                {
                    var msg = "Item could not be found.";
                    var valresult = new ValidationResult(msg, this, "Error", null, null);

                    validation.AddResult(valresult);
                }
            }
            catch (DataException e)
            {
                _logger.Error(e);

                var msg = "There was an issue processing the request, please try again";
                var valresult = new ValidationResult(msg, this, "Error", null, null);

                validation.AddResult(valresult);
            }

            var results = new PlankPostResponse<TEntity> { Item = entity, ValidationResults = Mapping<TEntity>.Mapper.Map<PlankValidationResults>(validation) };
            _logger.Info(results.ToJson());

            return results;
        }

        #endregion

        #region PRIVATE METHODS

        private ValidationResults ValidateEntity(TEntity entity)
        {
            var validator  = ValidationFactory.CreateValidator<TEntity>();
            var validation = validator.Validate(entity);

            return validation;
        }

        #endregion
    }
}
