import { useState } from "react";
import { useParams } from "react-router";
import { createPortal } from "react-dom";
import ParticipantCard from "@components/common/participant-card/ParticipantCard";
import ParticipantDetailsModal from "@components/common/modals/participant-details-modal/ParticipantDetailsModal";
import Button from "@components/common/button/Button";
import type { Participant } from "@types/api";
import {
  MAX_PARTICIPANTS_NUMBER,
  generateParticipantLink,
} from "@utils/general";
import { type ParticipantsListProps, type PersonalInformation, type ParticipantToDelete } from "./types";
import "./ParticipantsList.scss";

const ParticipantsList = ({ participants, onDeleteParticipant }: ParticipantsListProps) => {
  const { userCode } = useParams();
  const [selectedParticipant, setSelectedParticipant] =
    useState<PersonalInformation | null>(null);
  const [participantToDelete, setParticipantToDelete] =
    useState<ParticipantToDelete | null>(null);

  const admin = participants?.find((participant) => participant?.isAdmin);
  const restParticipants = participants?.filter(
    (participant) => !participant?.isAdmin,
  );

  const isParticipantsMoreThanTen = participants.length > 10;

  const handleInfoButtonClick = (participant: Participant) => {
    const personalInfoData: PersonalInformation = {
      firstName: participant.firstName,
      lastName: participant.lastName,
      phone: participant.phone,
      deliveryInfo: participant.deliveryInfo,
      email: participant.email,
      link: generateParticipantLink(participant.userCode),
    };
    setSelectedParticipant(personalInfoData);
  };

  const handleModalClose = () => setSelectedParticipant(null);

  const handleDeleteButtonClick = (participant: Participant) => {
    setParticipantToDelete({
      id: participant.id,
      firstName: participant.firstName,
      lastName: participant.lastName,
    });
  };

  const handleDeleteConfirm = () => {
    if (participantToDelete && onDeleteParticipant) {
      onDeleteParticipant(participantToDelete.id);
    }
    setParticipantToDelete(null);
  };

  const handleDeleteCancel = () => {
    setParticipantToDelete(null);
  };

  return (
    <div
      className={`participant-list ${isParticipantsMoreThanTen ? "participant-list--shift-bg-image" : ""}`}
    >
      <div
        className={`participant-list__content ${isParticipantsMoreThanTen ? "participant-list__content--extra-padding" : ""}`}
      >
        <div className="participant-list-header">
          <h3 className="participant-list-header__title">Who’s Playing?</h3>

          <span className="participant-list-counter__current">
            {participants?.length ?? 0}/
          </span>

          <span className="participant-list-counter__max">
            {MAX_PARTICIPANTS_NUMBER}
          </span>
        </div>

        <div className="participant-list__cards">
          {admin ? (
            <ParticipantCard
              key={admin?.id}
              firstName={admin?.firstName}
              lastName={admin?.lastName}
              isCurrentUser={userCode === admin?.userCode}
              isAdmin={admin?.isAdmin}
              isCurrentUserAdmin={userCode === admin?.userCode}
              adminInfo={`${admin?.phone}${admin?.email ? `\n${admin?.email}` : ""}`}
              participantLink={generateParticipantLink(admin?.userCode)}
            />
          ) : null}

          {restParticipants?.map((user) => (
            <ParticipantCard
              key={user?.id}
              firstName={user?.firstName}
              lastName={user?.lastName}
              isCurrentUser={userCode === user?.userCode}
              isCurrentUserAdmin={userCode === admin?.userCode}
              participantLink={generateParticipantLink(user?.userCode)}
              onInfoButtonClick={
                userCode === admin?.userCode && userCode !== user?.userCode
                  ? () => handleInfoButtonClick(user)
                  : undefined
              }
              onDeleteButtonClick={
                userCode === admin?.userCode && userCode !== user?.userCode && onDeleteParticipant
                  ? () => handleDeleteButtonClick(user)
                  : undefined
              }
            />
          ))}
        </div>

        {selectedParticipant ? (
          <ParticipantDetailsModal
            isOpen={!!selectedParticipant}
            onClose={handleModalClose}
            personalInfoData={selectedParticipant}
          />
        ) : null}

        {participantToDelete && createPortal(
          <div className="modal-container">
            <div className="modal" style={{ maxWidth: '450px' }}>
              <h3 className="modal__title">Confirm Deletion</h3>
              <p className="modal__description" style={{ marginTop: '20px', marginBottom: '30px' }}>
                Are you sure you want to remove {participantToDelete.firstName} {participantToDelete.lastName} from the room?
              </p>
              <div style={{ display: 'flex', gap: '10px', justifyContent: 'center' }}>
                <Button variant="secondary" size="medium" onClick={handleDeleteCancel}>
                  Cancel
                </Button>
                <Button variant="primary" size="medium" onClick={handleDeleteConfirm}>
                  Delete
                </Button>
              </div>
            </div>
          </div>,
          document.body
        )}
      </div>
    </div>
  );
};

export default ParticipantsList;
