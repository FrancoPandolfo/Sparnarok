import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import { QuestCard } from './QuestCard';
import { QuestState, QuestDifficulty } from '../types';
import { usePartyMembersStore } from '../../parties/usePartyMembers';
import { useQuestStore } from '../useQuestStore';

describe('QuestCard', () => {
  const mockQuest = {
    id: 'quest-123',
    title: 'Test Quest Title',
    description: 'A test description',
    difficulty: QuestDifficulty.D,
    state: QuestState.InProgress,
    assigneeId: 'user-hero',
    assigneeName: 'Merlin',
    rewards: [{ skillCategoryId: 'magic', xpAmount: 50 }],
    createdAt: ''
  };

  beforeEach(() => {
    usePartyMembersStore.setState({
      members: [
        { id: 'user-hero', username: 'Merlin', role: 'Wizard' },
        { id: 'user-knight', username: 'Lancelot', role: 'Knight' }
      ]
    });
    vi.clearAllMocks();
  });

  const renderCard = (quest = mockQuest) => {
    return render(
      <DragDropContext onDragEnd={() => {}}>
        <Droppable droppableId="col-1">
          {(provided) => (
            <div ref={provided.innerRef} {...provided.droppableProps}>
              <QuestCard quest={quest} index={0} />
              {provided.placeholder}
            </div>
          )}
        </Droppable>
      </DragDropContext>
    );
  };

  it('renders assignee initial in the avatar circle', () => {
    renderCard();
    expect(screen.getByText('Test Quest Title')).toBeDefined();
    expect(screen.getByText('M')).toBeDefined();
  });

  it('opens contextual menu on avatar click and allows reassignment', () => {
    const assignSpy = vi.fn();
    useQuestStore.setState({ assignQuest: assignSpy });

    renderCard();
    const avatarBtn = screen.getByLabelText('Asignado a Merlin');
    fireEvent.click(avatarBtn);

    expect(screen.getByText('Asignar Héroe')).toBeDefined();
    expect(screen.getByText('Lancelot')).toBeDefined();

    fireEvent.click(screen.getByText('Lancelot'));
    expect(assignSpy).toHaveBeenCalledWith('quest-123', 'user-knight', 'Lancelot');
  });
});
