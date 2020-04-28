﻿using Microsoft.Practices.EnterpriseLibrary.Validation;
using Plank.Net.Contracts;
using Plank.Net.Data;
using Plank.Net.Models;
using Plank.Net.Profiles;
using Serialize.Linq.Serializers;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Plank.Net.Managers
{
    public sealed class PlankManager<TEntity> : IManager<TEntity> where TEntity : class, IEntity
    {
        #region MEMBERS

        private readonly IRepository<TEntity> _repository;
        private readonly ILogger _logger;

        private readonly string _defaultErrorMessage;
        private readonly string _defaultItemNotFoundMessage;

        #endregion

        #region CONSTRUCTORS

        public PlankManager(DbContext context)
            : this(new PlankRepository<TEntity>(context), new PlankLogger<TEntity>())
        {

        }

        public PlankManager(IRepository<TEntity> repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger     = logger ?? throw new ArgumentNullException(nameof(logger));

            _defaultErrorMessage = "There was an issue processing the request, see the plank logs for details";
            _defaultItemNotFoundMessage = "Item could not be found";
        }

        #endregion

        #region METHODS

        public async Task<PlankPostResponse<TEntity>> AddAsync(TEntity item)
        {
            _logger.Info(item.ToJson());

            var validation = item.Validate();

            if (validation.IsValid)
            {
                try
                {
                    await _repository.AddAsync(item).ConfigureAwait(false);
                }
                catch (DataException e)
                {
                    _logger.Error(e);

                    validation.AddResult(new ValidationResult(_defaultErrorMessage, this, "Error", null, null));
                }
            }

            var results = new PlankPostResponse<TEntity>(Mapping<TEntity>.Mapper.Map<PlankValidationResultCollection>(validation)) 
            { 
                Item = item 
            };
            
            _logger.Info(results.ToJson());

            return results;
        }

        public async Task<PlankDeleteResponse> DeleteAsync(int id)
        {
            _logger.Info(id);

            var validation = new ValidationResults();

            try
            {
                if (await _repository.GetAsync(id).ConfigureAwait(false) != null)
                {
                    await _repository.DeleteAsync(id).ConfigureAwait(false);
                }
            }
            catch (DataException e)
            {
                _logger.Error(e);

                validation.AddResult(new ValidationResult(_defaultErrorMessage, this, "Error", null, null));
            }

            var results = new PlankDeleteResponse(Mapping<TEntity>.Mapper.Map<PlankValidationResultCollection>(validation)) 
            { 
                Id = id 
            };

            _logger.Info(results.ToJson());
            return results;
        }

        public async Task<PlankGetResponse<TEntity>> GetAsync(int id)
        {
            _logger.Info(id);
            PlankGetResponse<TEntity> result;

            try
            {
                var item = await _repository.GetAsync(id).ConfigureAwait(false);
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
                    Message = _defaultErrorMessage
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
                var pagedList  = await _repository.SearchAsync(expression, pageNumber, pageSize).ConfigureAwait(false);
                result         = Mapping<TEntity>.Mapper.Map<PlankEnumerableResponse<TEntity>>(pagedList);
                result.IsValid = true;
            }
            catch(DataException e)
            {
                _logger.Error(e);
                result = new PlankEnumerableResponse<TEntity>()
                {
                    IsValid = false,
                    Message = _defaultErrorMessage
                };
            }

            _logger.Info(result.ToJson());
            return result;
        }

        public async Task<PlankPostResponse<TEntity>> UpdateAsync(TEntity item)
        {
            _logger.Info(item.ToJson());

            TEntity existing = null;
            var validation = item.Validate();

            if (validation.IsValid)
            {
                try
                {
                    existing = await _repository.GetAsync(item.Id).ConfigureAwait(false);
                    if (existing != null)
                    {
                        foreach (var p in item.GetProperties())
                        {
                            p.SetValue(existing, p.GetValue(item));
                        };

                        await _repository.UpdateAsync(existing).ConfigureAwait(false);
                    }
                    else
                    {
                        validation.AddResult(new ValidationResult(_defaultItemNotFoundMessage, this, "Error", null, null));
                    }
                }
                catch (DataException e)
                {
                    _logger.Error(e);

                    validation.AddResult(new ValidationResult(_defaultErrorMessage, this, "Error", null, null));
                }
            }

            var results = new PlankPostResponse<TEntity>(Mapping<TEntity>.Mapper.Map<PlankValidationResultCollection>(validation)) 
            { 
                Item = existing 
            };

            _logger.Info(results.ToJson());

            return results;
        }

        public async Task<PlankPostResponse<TEntity>> UpdateAsync(TEntity item, params Expression<Func<TEntity, object>>[] properties)
        {
            _logger.Info(item.ToJson());

            TEntity existing = null;
            var validation = new ValidationResults();

            try
            {
                existing = item != null ? await _repository.GetAsync(item.Id).ConfigureAwait(false) : null;
                if (existing != null)
                {
                    // Assign values from item to the existing entity
                    foreach (var p in properties)
                    {
                        var operand = p.Body as MemberExpression ?? (p.Body as UnaryExpression).Operand as MemberExpression;
                        existing.GetType().GetProperty(operand.Member.Name).SetValue(existing, item.GetType().GetProperty(operand.Member.Name).GetValue(item));
                    }

                    // Assign values from existing back to item for validation
                    foreach (var p in item.GetProperties())
                    {
                        item.GetType().GetProperty(p.Name).SetValue(item, existing.GetType().GetProperty(p.Name).GetValue(existing));
                    }

                    validation = item.Validate();
                    if(validation.IsValid)
                    {
                        await _repository.UpdateAsync(existing).ConfigureAwait(false);
                    }
                }
                else
                {
                    validation.AddResult(new ValidationResult(_defaultItemNotFoundMessage, this, "Error", null, null));
                }
            }
            catch (DataException e)
            {
                _logger.Error(e);

                validation.AddResult(new ValidationResult(_defaultErrorMessage, this, "Error", null, null));
            }

            var results = new PlankPostResponse<TEntity>(Mapping<TEntity>.Mapper.Map<PlankValidationResultCollection>(validation)) 
            { 
                Item = existing 
            };

            _logger.Info(results.ToJson());

            return results;
        }

        #endregion
    }
}