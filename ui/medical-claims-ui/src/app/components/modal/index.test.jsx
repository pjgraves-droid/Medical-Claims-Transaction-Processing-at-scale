import { fireEvent, render, screen } from '@testing-library/react';
import Modal from './index';

describe('Modal', () => {
  it('renders nothing while hidden', () => {
    const { container } = render(<Modal showModal={false} setShowModal={jest.fn()} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('calls close and save callbacks', () => {
    const setShowModal = jest.fn();
    const onClose = jest.fn();
    const onSave = jest.fn();
    render(
      <Modal showModal setShowModal={setShowModal} onClose={onClose} onSave={onSave} title="Edit">
        Body
      </Modal>
    );
    fireEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(onClose).toHaveBeenCalled();
    expect(setShowModal).toHaveBeenCalledWith(false);
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(onSave).toHaveBeenCalled();
  });

  it('hides the save button when requested', () => {
    render(<Modal showModal setShowModal={jest.fn()} showSaveButton={false} title="Read only">Body</Modal>);
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });
});
