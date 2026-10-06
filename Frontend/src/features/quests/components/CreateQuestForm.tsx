
import { useTranslation } from 'react-i18next';

export const CreateQuestForm = () => {
  const { t } = useTranslation();

  return (
    <div>
      <h2>{t('quest.create.title')}</h2>
      <form aria-label="form-create-quest">
        <div>
          <label htmlFor="questName">{t('quest.create.nameLabel')}</label>
          <input id="questName" type="text" />
        </div>
        <div>
          <label htmlFor="questDesc">{t('quest.create.descLabel')}</label>
          <textarea id="questDesc"></textarea>
        </div>
        <button type="submit">{t('quest.create.submitBtn')}</button>
      </form>
    </div>
  );
};
